#### 0.12.14 - 18.05.2017
* Fix gitversion.yml

#### 0.12.13 - 27.04.2017
* Removes custom nuget source loading

#### 0.12.12 - 04.04.2017
* Automatically creates release_notes.md and gitversion.yml file if missing

#### 0.12.11 - 29.03.2017
* Does not include `.pdb` and `.xml` file in deploy packages

#### 0.12.10 - 08.12.2016
* Does not include `.pdb` file in nuget packages

#### 0.12.9 - 28.11.2016
* Removes the dependency for Nuget.Core

#### 0.12.8 - 18.11.2016
* Allows you to use `src\appname\appname.rn.md` for release notes by default

#### 0.12.7 - 09.11.2016
* Fixes version comparison bug

#### 0.12.6 - 09.11.2016
* Fixes version comparison bug

#### 0.12.5 - 01.11.2016
* Should be stable now

#### 0.12.4 - 09.10.2016
* Uses double build script

#### 0.12.3 - 08.10.2016
* Complete refactor. OMG

#### 0.12.2 - 07.10.2016
* Displays proper error messages

#### 0.12.1 - 07.10.2016
* Publishing a release is triggered by supplying a release notes record

#### 0.12.0 - 07.10.2016
* Adds support for multiple packages withing the samo git repository

#### 0.11.3 - 22.07.2016
* Uses `File.Exists` instead of `TestFile`

#### 0.11.2 - 21.07.2016
* Properly assign git version tag

#### 0.11.1 - 21.07.2016
* Uses `File.Exists` instead of `TestFile`

#### 0.11.0 - 20.07.2016
* Updated packages
* Rework the workflow. Now restoring packages has to be invoked explicitly because it is not part of the target execution chain because usually we have more than one project which is built as part of the script. This is also why the Release target is also out of the chain

#### 0.11.0-beta0003 - 28.03.2016
* Properly build lib package when output dir contains folders

#### 0.11.0-beta0002 - 29.01.2016
* Fixed vNext build example.

#### 0.11.0-beta0001 - 29.01.2016
* Added support for building vNext projects.

#### 0.10.0 - 07.01.2016
* Run Machine Specifications tests

#### 0.9.0 - 12.11.2015
* Add build definition template for TFS

#### 0.8.0 - 11.11.2015
* Now using GitVersion for auto-generating NuGet version.
* Auto-generating AssemblyInfo files.

#### 0.7.5 - 25.09.2015
* Properly invoke command with proper parameters in Init.ps1

#### 0.7.4 - 25.09.2015
* Properly invoke command to install dependencies in Init.ps1

#### 0.7.3 - 25.09.2015
* Add param($installPath, $toolsPath, $package, $project) in Init.ps1

#### 0.7.2 - 25.09.2015
* Fix errors in Init.ps1

#### 0.7.1 - 25.09.2015
* Try to install dependencies via Init.ps1

#### 0.7.0 - 25.09.2015
* Make FAKE and Nuget.Core internal references to NYX.
* Fixes #1

#### 0.6.7 - 07.06.2015
* Target https://www.nuget.org/api/v2 as default nuget endpoint

#### 0.6.6 - 07.06.2015
* Target https://www.nuget.org/api/v2 as default nuget endpoint

#### 0.6.5 - 02.06.2015
* Fix bower package restore

#### 0.6.4 - 28.05.2015
* Do not add dependencies when building Tool package

#### 0.6.3 - 28.05.2015
* Do not add dependencies when building Tool package

#### 0.6.2 - 28.05.2015
* Properly create nuget packages

#### 0.6.1 - 28.05.2015
* Fix bower package restore

#### 0.6.0 - 28.05.2015
* Add conventions when building packages

#### 0.5.0 - 27.05.2015
* Reorganize the script

#### 0.4.0 - 26.05.2015
* Republish

#### 0.3.2 - 26.05.2015
* Add deployment folder when building file package

#### 0.3.1 - 25.05.2015
* Fix the path where nyx looks for nuspec

#### 0.3.0 - 25.05.2015
* Move the nuspec file inside the app

#### 0.2.2 - 22.05.2015
* Republish 0.2.1

#### 0.2.1 - 22.05.2015
* Fix relative paths to Fake and nuget

#### 0.2.0 - 22.05.2015
* Move all files to Tools directory when building nuget

#### 0.1.1 - 22.05.2015
* Publish package when using CreateFileNuget

#### 0.1.0 - 22.05.2015
* Initial release
