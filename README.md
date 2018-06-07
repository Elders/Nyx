Build script which only purpose is to provide out of the box solution most nasty tasks. For example
- build c# projects (FAKE)
- apply and track versioning (github flow)
- create build artifact and publish it (nuget)

# Enable SourceLink in your project

Add the following setup in your project file

```
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DebugType>portable</DebugType>
  <DebugSymbols>true</DebugSymbols>
  <EmbedSources>true</EmbedSources>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <DebugType>portable</DebugType>
  <DebugSymbols>true</DebugSymbols>
  <EmbedSources>true</EmbedSources>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="SourceLink.Create.CommandLine" Version="2.8.0" PrivateAssets="All" />
</ItemGroup>
```

To test your assembly with sourcelink use `dotnet-sourcelink` tool

```
<DotNetCliToolReference Include="dotnet-sourcelink" Version="2.8.0" />
```


# How to use in VS

Go to https://github.com/ctaggart/SourceLink and follow the first part. It should take 2-3 min max to enable SourceLink for VS2017 and debug flawlessly
