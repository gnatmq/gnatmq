
MSBuild.exe GnatMQ.sln /p:Configuration=Release

IF NOT EXIST ".\Build\Packages" MKDIR ".\Build\Packages"

.\Tools\NuGet\NuGet.exe pack GnatMQ.nuspec -OutputDirectory ".\Build\Packages"
