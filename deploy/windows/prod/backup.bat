:: Rsc
:: 23-09-2021
:: Generate backUps of the files that we delete on the server

@ECHO OFF

SET _path=C:\inetpub\Rollbacks\%date:~-7,2%-%date:~-10,2%-%date:~-4,4%\Nft%time:~3,2%

if not exist %_path% md %_path%

cd C:\inetpub\vhosts\coinpay.cr\nft.coinpay.cr

xcopy * %_path% /S /Y

exit