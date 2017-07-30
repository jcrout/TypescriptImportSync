# TypescriptImportSync
Manages Typescript relative import paths when files are modified

Three projects are included in this repository:
1. Core library
2. Console application
⋅⋅1. Takes one unnamed command line argument for the path to watch
3. Visual Studio 2017 extension project


# What this library does

### The problem we're trying to solve

If you have a files in a structure like this:
 /src/app/app.module.ts <br/>
 /src/app/services/service1.service.ts <br/>
 /src/app/components/testcomponent/test.component.ts <br/>

and file 'app.module.ts' contains imports such as :
import { Service1 } from '/services/service1.service'; <br/>
import { TestComponent } from '/testcomponent/test.component'; <br/>
