import express from 'express'
import cors from 'cors'
import { ImapFlow } from 'imapflow'

const app = express()
const PORT = Number(process.env.MAIL_NOTIFIER_PORT || 5178)

app.use(cors())
app.use(express.json({ limit: '1mb' }))

const PROVIDERS = {
  qq: { host: 'imap.qq.com', port: 993, secure: true, inboxUrl: 'https://mail.qq.com/' },
  gmail: { host: 'imap.gmail.com', port: 993, secure: true, inboxUrl: 'https://mail.google.com/mail/u/0/#inbox' },
  outlook: { host: 'outlook.office365.com', port: 993, secure: true, inboxUrl: 'https://outlook.live.com/mail/0/inbox' },
  '163': { host: 'imap.163.com', port: 993, secure: true, inboxUrl: 'https://mail.163.com/' },
}

function normalizeConfig(raw = {}) {
  const preset = PROVIDERS[raw.provider] || {}
  return {
    provider: raw.provider || 'custom',
    email: String(raw.email || '').trim(),
    password: String(raw.password || ''),
    host: String(raw.host || preset.host || '').trim(),
    port: Number(raw.port || preset.port || 993),
    secure: raw.secure !== false,
    mailbox: String(raw.mailbox || 'INBOX').trim() || 'INBOX',
    inboxUrl: String(raw.inboxUrl || preset.inboxUrl || 'https://mail.google.com/').trim(),
  }
}

function publicConfig(config) {
  const { password, ...safe } = config
  return { ...safe, bound: Boolean(config.email && config.password && config.host) }
}

async function withClient(config, fn) {
  const client = new ImapFlow({
    host: config.host,
    port: config.port,
    secure: config.secure,
    auth: { user: config.email, pass: config.password },
    logger: false,
  })

  await client.connect()
  try {
    return await fn(client)
  } finally {
    await client.logout().catch(() => {})
  }
}

function serializeMessage(msg) {
  return {
    uid: msg.uid,
    seq: msg.seq,
    subject: msg.envelope?.subject || '(无主题)',
    from: (msg.envelope?.from || []).map(item => item.name || item.address).filter(Boolean).join(', ') || '-',
    date: msg.envelope?.date ? new Date(msg.envelope.date).toISOString() : new Date().toISOString(),
    flags: Array.from(msg.flags || []),
    seen: msg.flags?.has('\\Seen') || false,
  }
}

app.get('/api/health', (_req, res) => {
  res.json({ status: 'ok', service: 'mail-notifier-beta' })
})

app.post('/api/test', async (req, res) => {
  const config = normalizeConfig(req.body)
  if (!config.email || !config.password || !config.host) {
    res.status(400).json({ status: 'error', message: '请填写邮箱、授权码和 IMAP 主机' })
    return
  }

  try {
    const result = await withClient(config, async client => {
      const lock = await client.getMailboxLock(config.mailbox)
      try {
        const status = await client.status(config.mailbox, { messages: true, unseen: true, uidNext: true })
        return status
      } finally {
        lock.release()
      }
    })
    res.json({ status: 'success', config: publicConfig(config), mailbox: result })
  } catch (error) {
    res.status(502).json({ status: 'error', message: error.message || 'IMAP 连接失败' })
  }
})

app.post('/api/check', async (req, res) => {
  const config = normalizeConfig(req.body.config)
  const sinceUid = Number(req.body.sinceUid || 0)
  const limit = Math.max(1, Math.min(Number(req.body.limit || 30), 80))

  if (!config.email || !config.password || !config.host) {
    res.status(400).json({ status: 'error', message: '邮箱未绑定' })
    return
  }

  try {
    const data = await withClient(config, async client => {
      const lock = await client.getMailboxLock(config.mailbox)
      try {
        const status = await client.status(config.mailbox, { messages: true, unseen: true, uidNext: true })
        const range = status.messages > limit ? `${status.messages - limit + 1}:*` : '1:*'
        const messages = []
        for await (const msg of client.fetch(range, { uid: true, envelope: true, flags: true })) {
          messages.push(serializeMessage(msg))
        }
        messages.sort((a, b) => b.uid - a.uid)
        const newMessages = sinceUid ? messages.filter(item => item.uid > sinceUid) : []
        return {
          mailbox: status,
          latestUid: messages[0]?.uid || sinceUid || 0,
          messages,
          newMessages,
        }
      } finally {
        lock.release()
      }
    })
    res.json({ status: 'success', ...data })
  } catch (error) {
    res.status(502).json({ status: 'error', message: error.message || '查收失败' })
  }
})

app.listen(PORT, () => {
  console.log(`mail-notifier-beta api listening on http://localhost:${PORT}`)
})
