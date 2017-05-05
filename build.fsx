#I "packages/FAKE/tools"
#r "FakeLib.dll" // include Fake lib
#load "blog.fsx"

open Fake

open System
open System.Diagnostics
open System.IO

//dev
// let siteUrl = "http://127.0.0.1:8000/site"
// //dev online
// let siteUrl = "http://bittacklr.bitballoon.com"
//production
let siteUrl = "http://bittacklr.be"

let siteDir = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "site"))
let siteBlogDir = DirectoryInfo(Path.Combine(siteDir.FullName, "blog"))
let srcDir = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "src"))
let templatesDir = DirectoryInfo(Path.Combine(srcDir.FullName, "templates"))
let blogPostsDir = DirectoryInfo(Path.Combine(srcDir.FullName, "posts"))

Target "Clean" (fun () -> DeleteDir siteDir.FullName)

Target "CopyStaticContent" (fun () -> 
    !! "src/*.html"
    ++ "src/*.css"
    ++ "src/*.js"
    ++ "src/*.vcf"
    ++ "src/images/*.svg"
    ++ "src/fonts/*.eot"
    ++ "src/fonts/*.svg"
    ++ "src/fonts/*.ttf"
    ++ "src/fonts/*.woff"
    ++ "src/fonts/*.woff2"
    |> Seq.iter (fun path -> 
        CopyFileWithSubfolder srcDir.FullName siteDir.FullName path
    )
)

Target "Init" (fun () -> 
    CreateDir siteDir.FullName
)

Target "Build" (fun () -> 
    let settings : Blog.BlogSettings =
        {
            SiteUrl = siteUrl
            SiteDir = siteDir
            SiteBlogDir = siteBlogDir
            BlogBaseUrl = sprintf "%s/blog" siteUrl
            SrcDir = srcDir
            BlogPostsDir = blogPostsDir
            TemplatesDir = templatesDir
        }
    Blog.generate settings
)

"Clean"
==> "Init"
==> "CopyStaticContent"
==> "Build"

RunTargetOrDefault "Build"