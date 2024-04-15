:: Rsc
:: 23-09-2021
:: Generate deploy nft - coinpay.cr

@ECHO OFF

SET _current=%cd%
SET _path=C:\inetpub\vhosts\coinpay.cr\nft.coinpay.cr

cd %_path%\

rd /S /Q %_path%

cd %_current%
cd CoinpayNFT\bin\app.publish

:: add news files 
:: 
xcopy * %_path% /S

exit
