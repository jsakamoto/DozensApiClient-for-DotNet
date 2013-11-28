Dozens API Client for .NET (and COM)
==========================

Summary / 概要
--------------
クラウド時代に対応した DNSサービス "Dozens" http://dozens.jp の REST API を呼び出して、
ゾーンやレコードの追加・取得・変更・削除を行うことができる、C# で書かれたクライアントアクセスライブラリです。


C# からはもちろん、F# スクリプトや Windows PowerShell などの .NET Framework を利用可能な処理系から利用可能なほか、
COM サーバーとして登録することで、VBSvript や JScript、VB6 などからも利用することが可能です。

NuGet にもパッケージとして登録済みです。

http://nuget.org/List/Packages/DozensAPIClient

ですので、例えばパッケージマネージャコンソールから以下のコマンドを実行するなどの手順により、
容易に自分のプロジェクトに Dozens API Client を追加することができます。

    PM> Install-Package DozensAPIClient

System Requirements / システム要件
-----------------------------------
.NET Framework 3.5

Notice / 注意
-------------
This class library dose not work at ".NET Framework 3.5 Client Profile".

「.NET Framework 3.5 Client Profile」上では動作しません。

Usage / 使い方 - YouTube Videos
--------------------------------

### F# Script Edition / F# スクリプト版

http://youtu.be/ziklLtz08og

### C# Console App Edition / C# コンソールアプリ版

http://youtu.be/it_WeNAeeds

### Windows PoerShell Edition / Windows PowerShell 版

http://youtu.be/3EQVmKplISo

### VBScript Edition / VBScript 版

http://youtu.be/DDLL8fpPAa4


### Extra - Microsoft Small Basic

"Dozens API Client for Small Basic" http://dozens4smallbasic.codeplex.com をインストールすると、
Microsoft Small Basic http://smallbasic.com/ からも Dozens の REST API を呼び出して自由に DNS を構成することができます。

http://youtu.be/a5bKMkI0P4M



