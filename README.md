# TypescriptImportSync
Manages Typescript relative import paths when files are modified

Three projects are included in this repository:
1. Core library
2. Console application
⋅⋅1. Takes one unnamed command line argument for the path to watch
3. Visual Studio 2017 extension project


# What this library does

### The problem we're trying to solve

If you have a files in a structure like this: <br/>
 <p>/src/app/app.module.ts </p>
 <p>/src/app/services/service1.service.ts </p>
 <p>/src/app/components/testcomponent/test.component.ts </p>

and file 'app.module.ts' contains imports such as :
import { Service1 } from '/services/service1.service'; <br/>
import { TestComponent } from '/testcomponent/test.component'; <br/>

If you move the 'app.module.ts' file to '/src/app/modules/app.module.ts', you've now got a problem: your relative imports are incorrect. To resolve this, you would need to change each import to:<br/>
import { Service1 } from '<strong>..</strong>/services/service1.service'; <br/>
import { TestComponent } from '<strong>..</strong>/testcomponent/test.component'; <br/>

Notice the '..' at the beginning of the imports. This means the relative path should first go up one level before going down further directories. Updating one file like this is annoying, but not the end of the world. But when you move several files at once, such as when moving a whole directory, then it can be burdensome.
