pushd $PSScriptRoot/ScaleableHostedService/ScaleableHostedService

try {
    dotnet clean -c Release
    pushd ..
    dotnet build
    dotnet test
    popd
    dotnet pack -c Release
}
finally {
    popd
}