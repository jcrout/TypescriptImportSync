# TypescriptImportSync
Manages Typescript relative import paths when files are modified

Three projects are included in this repository:
1. Core library
2. Console application
    * Takes one unnamed command line argument for the path to watch
3. Visual Studio 2017 extension project


# What this library does

### The problem we're trying to solve

If you have a files in a structure like this: <br/>
 ```/src/app/app.module.ts
 /src/app/services/service1.service.ts
 /src/app/components/testcomponent/test.component.ts
 ```

and file 'app.module.ts' contains imports such as :
```typescript
import { Service1 } from '/services/service1.service';
import { TestComponent } from '/testcomponent/test.component';
```

If you move the 'app.module.ts' file to '/src/app/modules/app.module.ts', you've now got a problem: your relative imports are incorrect. To resolve this, you would need to change each import to:<br/>
```typescript
import { Service1 } from '../services/service1.service';
import { TestComponent } from '../testcomponent/test.component';
```

Notice the '..' at the beginning of the imports. This means the relative path should first go up one level before going down further directories. Updating one file like this is annoying, but not the end of the world. But when you move several files at once, such as when moving a whole directory, then it can be burdensome. Also, when you have other files that reference any of the moved files, you now have to update multiple files at once.

What we need is a way to automatically have those relative imports updated when we move/rename files/directories.

### TypescriptImportSync core library

When this process is running, either via command line, VS Extension, or by some other means, this process will be taken care of for you. 
When files or folders are moved or renamed, the following happens:
1. Each moved file has its own relative imports examined
⋅⋅1. If a new/correct relative path can be determined, the path portion of the import line will be updated
⋅⋅⋅⋅1. This means that the style and spacing you have applied to the import line will still be preserved
2. Every file that references the changed files will also be updated in a similar manner if necessary

This process prefers to use a batch-oriented style, because several files could be updated at once (such as when cutting/pasting many files or a whole directory full of files), and this can lead to race conditions when examining and updating files that could be modified during the process.

### TypescriptImportSync is lightweight

This process will scan each of the .ts files within the specified directory just once initially, to store ONLY the relative import lines and the exported objects. These are maintained in memory and are automatically updated whenever files change. Entire file contents are *not* stored in memory.
