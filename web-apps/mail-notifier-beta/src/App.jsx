import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react'

const API_BASE = 'http://localhost:5178'
const STORAGE_KEY = 'ikun_mail_notifier_beta_config'
const UID_KEY = 'ikun_mail_notifier_beta_latest_uid'

const PROVIDERS = [
  { id: 'qq', label: 'QQ 邮箱', host: 'imap.qq.com', inboxUrl: 'https://mail.qq.com/' },
  { id: 'gmail', label: 'Gmail', host: 'imap.gmail.com', inboxUrl: 'https://mail.google.com/mail/u/0/#inbox' },
  { id: 'outlook', label: 'Outlook', host: 'outlook.office365.com', inboxUrl: 'https://outlook.live.com/mail/0/inbox' },
  { id: '163', label: '163 邮箱', host: 'imap.163.com', inboxUrl: 'https://mail.163.com/' },
  { id: 'custom', label: '自定义 IMAP', host: '', inboxUrl: '' },
]

const DEFAULT_CONFIG = {
  provider: 'qq',
  email: '',
  password: '',
  host: 'imap.qq.com',
  port: 993,
  secure: true,
  mailbox: 'INBOX',
  inboxUrl: 'https://mail.qq.com/',
  intervalSec: 20,
}

function loadConfig() {
  try {
    return { ...DEFAULT_CONFIG, ...JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}') }
  } catch {
    return DEFAULT_CONFIG
  }
}

function formatTime(value) {
  try {
    return new Date(value).toLocaleString()
  } catch {
    return '-'
  }
}

function playAlertSound() {
  const AudioContext = window.AudioContext || window.webkitAudioContext
  if (!AudioContext) return
  const ctx = new AudioContext()
  const now = ctx.currentTime
  const gain = ctx.createGain()
  gain.connect(ctx.destination)
  gain.gain.setValueAtTime(0.0001, now)
  gain.gain.exponentialRampToValueAtTime(0.18, now + 0.02)
  gain.gain.exponentialRampToValueAtTime(0.0001, now + 1.15)

  ;[0, 0.22, 0.44].forEach((offset, index) => {
    const osc = ctx.createOscillator()
    osc.type = 'sine'
    osc.frequency.setValueAtTime(index === 1 ? 980 : 740, now + offset)
    osc.connect(gain)
    osc.start(now + offset)
    osc.stop(now + offset + 0.16)
  })

  setTimeout(() => ctx.close().catch(() => {}), 1500)
}

function App() {
  const [config, setConfig] = useState(loadConfig)
  const [connected, setConnected] = useState(false)
  const [running, setRunning] = useState(false)
  const [status, setStatus] = useState('等待绑定邮箱')
  const [messages, setMessages] = useState([])
  const [newQueue, setNewQueue] = useState([])
  const [latestUid, setLatestUid] = useState(() => Number(localStorage.getItem(UID_KEY) || 0))
  const [permission, setPermission] = useState(() => window.Notification?.permission || 'unsupported')
  const [checking, setChecking] = useState(false)
  const [lastCheck, setLastCheck] = useState('')
  const [error, setError] = useState('')
  const timerRef = useRef(null)

  const selectedProvider = useMemo(
    () => PROVIDERS.find(item => item.id === config.provider) || PROVIDERS[0],
    [config.provider],
  )

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(config))
  }, [config])

  useEffect(() => {
    localStorage.setItem(UID_KEY, String(latestUid || 0))
  }, [latestUid])

  const updateConfig = (key, value) => {
    setConfig(prev => ({ ...prev, [key]: value }))
  }

  const chooseProvider = (providerId) => {
    const provider = PROVIDERS.find(item => item.id === providerId) || PROVIDERS[0]
    setConfig(prev => ({
      ...prev,
      provider: provider.id,
      host: provider.host || prev.host,
      inboxUrl: provider.inboxUrl || prev.inboxUrl,
      port: 993,
      secure: true,
    }))
  }

  const requestPermission = async () => {
    if (!('Notification' in window)) {
      setPermission('unsupported')
      setStatus('当前浏览器不支持 Windows 通知')
      return
    }
    const result = await Notification.requestPermission()
    setPermission(result)
  }

  const emitNativeNotification = useCallback((mail) => {
    if (!('Notification' in window) || Notification.permission !== 'granted') return
    const notification = new Notification('IKUNANCE 新邮件', {
      body: `${mail.from}\n${mail.subject}`,
      tag: `ikun-mail-${mail.uid}`,
      requireInteraction: true,
      silent: true,
    })
    notification.onclick = () => {
      window.focus()
    }
  }, [])

  const checkMail = useCallback(async ({ baseline = false } = {}) => {
    setChecking(true)
    setError('')
    try {
      const response = await fetch(`${API_BASE}/api/check`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          config,
          sinceUid: baseline ? 0 : latestUid,
          limit: 40,
        }),
      })
      const data = await response.json()
      if (!response.ok || data.status !== 'success') {
        throw new Error(data.message || '查收失败')
      }
      setMessages(data.messages || [])
      setLatestUid(data.latestUid || 0)
      setLastCheck(new Date().toLocaleTimeString())
      setStatus(`已查收 ${data.messages?.length || 0} 封，未读 ${data.mailbox?.unseen || 0}`)

      const fresh = baseline ? [] : (data.newMessages || [])
      if (fresh.length > 0) {
        playAlertSound()
        fresh.slice().reverse().forEach(emitNativeNotification)
        setNewQueue(prev => [...prev, ...fresh].sort((a, b) => b.uid - a.uid))
      }
    } catch (err) {
      setError(err.message || '查收失败')
      setStatus('连接异常')
    } finally {
      setChecking(false)
    }
  }, [config, latestUid, emitNativeNotification])

  const testConnection = async () => {
    setChecking(true)
    setError('')
    try {
      const response = await fetch(`${API_BASE}/api/test`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config),
      })
      const data = await response.json()
      if (!response.ok || data.status !== 'success') throw new Error(data.message || '连接失败')
      setConnected(true)
      setStatus(`绑定成功 · ${data.mailbox?.messages || 0} 封邮件`)
      await checkMail({ baseline: true })
    } catch (err) {
      setConnected(false)
      setError(err.message || '连接失败')
      setStatus('绑定失败')
    } finally {
      setChecking(false)
    }
  }

  const toggleRunning = () => {
    setRunning(value => !value)
  }

  useEffect(() => {
    if (!running) {
      if (timerRef.current) clearInterval(timerRef.current)
      timerRef.current = null
      return
    }
    checkMail()
    timerRef.current = setInterval(() => checkMail(), Math.max(10, Number(config.intervalSec || 20)) * 1000)
    return () => {
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [running, config.intervalSec, checkMail])

  const currentAlert = newQueue[0]

  const confirmAndJump = () => {
    window.open(config.inboxUrl || selectedProvider.inboxUrl || 'https://mail.google.com/', '_blank', 'noopener,noreferrer')
    setNewQueue(prev => prev.slice(1))
  }

  const simulateNewMail = () => {
    const fake = {
      uid: Date.now(),
      from: 'IKUNANCE Beta Monitor',
      subject: `测试新邮件 ${new Date().toLocaleTimeString()}`,
      date: new Date().toISOString(),
      seen: false,
    }
    playAlertSound()
    emitNativeNotification(fake)
    setNewQueue(prev => [fake, ...prev])
    setMessages(prev => [fake, ...prev])
    setStatus('已触发测试新邮件')
  }

  return (
    <div className="ikun-mail-app">
      <header className="global-nav">
        <button className="brand-btn">IKUNANCE</button>
        <nav className="global-nav-links">
          <button className="active">邮件提示器</button>
          <button>Beta</button>
        </nav>
        <div className="global-nav-user">
          <span className={running ? 'live-pill live' : 'live-pill'}>{running ? '监听中' : '待机'}</span>
        </div>
      </header>

      <div className="app-container">
        <aside className="sidebar">
          <section className="control-group">
            <label className="label">邮箱类型</label>
            <div className="segmented">
              {PROVIDERS.map(provider => (
                <button key={provider.id} className={config.provider === provider.id ? 'active' : ''} onClick={() => chooseProvider(provider.id)}>
                  {provider.label}
                </button>
              ))}
            </div>
          </section>

          <section className="control-group">
            <label className="label">邮箱账号</label>
            <input className="bn-input" value={config.email} onChange={e => updateConfig('email', e.target.value)} placeholder="name@example.com" />
          </section>

          <section className="control-group">
            <label className="label">邮箱授权码</label>
            <input className="bn-input" type="password" value={config.password} onChange={e => updateConfig('password', e.target.value)} placeholder="IMAP/SMTP 授权码" />
          </section>

          <section className="control-group two-col">
            <div>
              <label className="label">IMAP 主机</label>
              <input className="bn-input" value={config.host} onChange={e => updateConfig('host', e.target.value)} />
            </div>
            <div>
              <label className="label">端口</label>
              <input className="bn-input" type="number" value={config.port} onChange={e => updateConfig('port', e.target.value)} />
            </div>
          </section>

          <section className="control-group">
            <label className="label">确认跳转地址</label>
            <input className="bn-input" value={config.inboxUrl} onChange={e => updateConfig('inboxUrl', e.target.value)} placeholder="https://mail.qq.com/" />
          </section>

          <section className="control-group two-col">
            <div>
              <label className="label">邮箱目录</label>
              <input className="bn-input" value={config.mailbox} onChange={e => updateConfig('mailbox', e.target.value)} />
            </div>
            <div>
              <label className="label">轮询秒数</label>
              <input className="bn-input" type="number" min="10" value={config.intervalSec} onChange={e => updateConfig('intervalSec', e.target.value)} />
            </div>
          </section>

          <section className="control-group button-stack">
            <button className="icon-btn login-btn" onClick={testConnection} disabled={checking}>{checking ? '检查中...' : '绑定并测试'}</button>
            <button className="icon-btn" onClick={requestPermission}>开启 Windows 通知</button>
            <button className="icon-btn" onClick={() => checkMail()} disabled={!connected || checking}>手动查收</button>
            <button className={running ? 'icon-btn danger' : 'icon-btn success'} onClick={toggleRunning} disabled={!connected}>{running ? '停止监听' : '开始监听'}</button>
            <button className="icon-btn" onClick={simulateNewMail}>模拟新邮件</button>
          </section>

          <section className="status-panel-large">
            <div className={running ? 'status-pulse' : 'status-pulse idle'} />
            <div className="status-title">{status}</div>
            <div className="status-sub">通知权限：{permission}</div>
            {lastCheck && <div className="status-sub">上次查收：{lastCheck}</div>}
          </section>
        </aside>

        <main className="main-content">
          <div className="tabs-header">
            <button className="tab-btn active">收件箱</button>
            <button className="tab-btn">新邮件必须确认跳转</button>
          </div>

          <div className="list-container">
            {error && <div className="error-bar">{error}</div>}
            <div className="summary-grid">
              <div className="summary-card">
                <span>绑定状态</span>
                <strong className={connected ? 'trend-bull' : 'trend-bear'}>{connected ? '已绑定' : '未绑定'}</strong>
              </div>
              <div className="summary-card">
                <span>邮件数量</span>
                <strong>{messages.length}</strong>
              </div>
              <div className="summary-card">
                <span>待确认新邮件</span>
                <strong className={newQueue.length ? 'trend-bear' : ''}>{newQueue.length}</strong>
              </div>
            </div>

            <table className="bn-table">
              <thead>
                <tr>
                  <th>时间</th>
                  <th>发件人</th>
                  <th>主题</th>
                  <th>状态</th>
                </tr>
              </thead>
              <tbody>
                {messages.length === 0 ? (
                  <tr><td colSpan="4" className="empty-cell">暂无邮件。绑定后点击手动查收，或用模拟新邮件测试核心流程。</td></tr>
                ) : messages.map(mail => (
                  <tr key={mail.uid}>
                    <td>{formatTime(mail.date)}</td>
                    <td><div className="pair-name">{mail.from}</div></td>
                    <td>{mail.subject}</td>
                    <td>{mail.seen ? <span className="signal-badge muted">已读</span> : <span className="signal-badge badge-yellow">未读</span>}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </main>
      </div>

      {newQueue.length > 0 && (
        <div className="notify-stack">
          {newQueue.slice(0, 3).map(mail => (
            <div key={mail.uid} className="notify-card locked">
              <div className="nc-sym">新邮件 · UID {mail.uid}</div>
              <div className="nc-info">{mail.from}</div>
              <div className="nc-action">{mail.subject}</div>
              <div className="nc-time">{formatTime(mail.date)}</div>
            </div>
          ))}
        </div>
      )}

      {currentAlert && (
        <div className="modal alert-lock" role="dialog" aria-modal="true" aria-labelledby="new-mail-title">
          <div className="modal-container lock-card">
            <div className="lock-icon">📧</div>
            <div id="new-mail-title" className="modal-title">收到新邮件，必须确认跳转</div>
            <div className="lock-copy">
              这个弹窗不会因为点击遮罩、按 Esc 或等待超时而关闭。必须手动点击下方按钮，跳转到邮箱后才会关闭当前提醒。
            </div>
            <div className="mail-preview">
              <div><span>发件人</span><strong>{currentAlert.from}</strong></div>
              <div><span>主题</span><strong>{currentAlert.subject}</strong></div>
              <div><span>时间</span><strong>{formatTime(currentAlert.date)}</strong></div>
            </div>
            <button className="icon-btn login-btn confirm-jump" onClick={confirmAndJump}>
              确认并跳转邮箱
            </button>
            {newQueue.length > 1 && <div className="status-sub">队列中还有 {newQueue.length - 1} 封新邮件待确认</div>}
          </div>
        </div>
      )}
    </div>
  )
}

export default App
