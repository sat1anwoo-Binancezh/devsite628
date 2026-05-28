邮件提醒器 Beta

启动方式：
双击 邮件提醒器Beta.exe

项目内容：
- 邮件提醒器Beta.exe：可直接运行的 Windows 程序
- 源码和资源：C# 源码、图标、Logo 等资源

使用要点：
- QQ/163 等邮箱通常需要授权码，不是网页登录密码。
- 点击“开始监听”后，只提醒之后新收到的邮件。
- 新邮件提醒必须点击“查阅邮箱并停止提醒”才会停止弹窗和提示音。
- 配置会保存到当前 Windows 用户目录：%APPDATA%\IKUNANCE\MailReminderBeta\settings.cfg
- 窗口外框会在支持的 Windows 版本上尝试启用 Acrylic 模糊；内容区保持深色监控台主题。
- 最小化或点击窗口关闭按钮会隐藏到 Windows 右下角托盘；双击托盘图标可恢复，右键菜单可退出程序。
- 检测到新邮件时会同时触发托盘气泡通知、强制确认弹窗和循环提示音。
- 运行日志：%APPDATA%\IKUNANCE\MailReminderBeta\activity.log
