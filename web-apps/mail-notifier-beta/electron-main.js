import { app, BrowserWindow, Menu, Tray, nativeImage, shell } from 'electron'
import { fileURLToPath } from 'node:url'
import path from 'node:path'
import { startServer } from './server/index.js'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const API_PORT = Number(process.env.MAIL_NOTIFIER_PORT || 5178)

let mainWindow
let tray
let server
let isQuitting = false

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1240,
    height: 820,
    minWidth: 980,
    minHeight: 640,
    backgroundColor: '#030812',
    title: 'IKUNANCE Mail Notifier Beta',
    autoHideMenuBar: true,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
    },
  })

  mainWindow.loadFile(path.join(__dirname, 'dist', 'index.html'))

  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url)
    return { action: 'deny' }
  })

  mainWindow.on('close', event => {
    if (isQuitting) return
    event.preventDefault()
    mainWindow.hide()
  })
}

function createTray() {
  const icon = nativeImage.createEmpty()
  tray = new Tray(icon)
  tray.setToolTip('IKUNANCE Mail Notifier Beta')
  tray.setContextMenu(Menu.buildFromTemplate([
    { label: '打开邮件提示器', click: () => { mainWindow?.show(); mainWindow?.focus() } },
    { label: '退出', click: () => { isQuitting = true; app.quit() } },
  ]))
  tray.on('click', () => {
    mainWindow?.show()
    mainWindow?.focus()
  })
}

app.whenReady().then(() => {
  server = startServer(API_PORT)
  createWindow()
  createTray()
})

app.on('window-all-closed', event => {
  event.preventDefault()
})

app.on('before-quit', () => {
  isQuitting = true
  server?.close?.()
})
