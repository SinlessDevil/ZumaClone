$BotToken = "8019763377:AAGDfNlIVzqU2Y_6z_pK5WQp3QRy78Qy0Aw"

$ChatId = "-1002676219002"

$Message = "✅ Билд завершен успешно! Готов к скачиванию."

$Body = @{
    chat_id = $ChatId
    text    = $Message
} | ConvertTo-Json -Depth 10

Invoke-RestMethod `
    -Uri "https://api.telegram.org/bot$BotToken/sendMessage" `
    -Method Post `
    -ContentType "application/json" `
    -Body ([System.Text.Encoding]::UTF8.GetBytes($Body))