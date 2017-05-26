#I "packages/FAKE/tools"
#r "FakeLib.dll" // include Fake lib
#load "blog.fsx"

open Fake

open System
open System.Diagnostics
open System.IO

open AngleSharp
open AngleSharp.Dom
open AngleSharp.Dom.Html
open AngleSharp.Extensions
open AngleSharp.Html
open AngleSharp.Parser.Html

//dev
// let siteUrl = "http://127.0.0.1:8000/site"
// //dev online
// let siteUrl = "http://bittacklr.bitballoon.com"
//production
let siteUrl = "http://bittacklr.be"

let thisDir = DirectoryInfo(__SOURCE_DIRECTORY__)
let siteDir = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "site"))
let siteBlogDir = DirectoryInfo(Path.Combine(siteDir.FullName, "blog"))
let siteImagesDir = (DirectoryInfo(Path.Combine(siteDir.FullName, "images")))
let srcDir = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "src"))
let templatesDir = DirectoryInfo(Path.Combine(srcDir.FullName, "templates"))
let blogPostsDir = DirectoryInfo(Path.Combine(srcDir.FullName, "posts"))

/// Settings
let private timeToComplete = TimeSpan.FromSeconds(5.0)
let private dotless = Path.Combine(Path.Combine(Path.Combine(Path.Combine(__SOURCE_DIRECTORY__, "packages"), "dotless"), "tool"), "dotless.compiler.exe")

Target "Clean" (fun () -> DeleteDir siteDir.FullName)

let private compileLess (workingDir: DirectoryInfo) (stylesheet: string) =
    let result = 
        ExecProcess (fun info ->
                        if isLinux then
                            info.FileName <- dotless
                        elif isWindows then
                            info.FileName <- dotless
                        else
                            failwith "Only linux and windows are supported at this moment in time."
                        info.Arguments <- sprintf "-m %s" stylesheet
                        info.WorkingDirectory <- workingDir.FullName
                    ) timeToComplete
    if result <> 0 then
        failwith "Could not compile less in a timely fashion."

let private minifySvg (configDir: DirectoryInfo) (workingDir: DirectoryInfo) =
    let result = 
        ExecProcess (fun info ->
                        if isLinux then
                            info.FileName <- "node"
                            //info.Arguments <- sprintf "../../node_modules/svgo/bin/svgo --enable=removeAttrs --config=%s -f %s" (Path.Combine(configDir.FullName, "svgo.yml")) workingDir.FullName
                            info.Arguments <- sprintf "../../node_modules/svgo/bin/svgo -f %s" workingDir.FullName
                            //removeAttrs svg:height svg:width svg:viewBox
                        elif isWindows then
                            info.FileName <- "node.exe"
                            //info.Arguments <- sprintf "..\\..\\node_modules\\svgo\\bin\\svgo --enable=removeAttrs --config=%s -f %s" (Path.Combine(configDir.FullName, "svgo.yml"))  workingDir.FullName
                            info.Arguments <- sprintf "..\\..\\node_modules\\svgo\\bin\\svgo -f %s" workingDir.FullName
                        else
                            failwith "Only linux and windows are supported at this moment in time."
                        info.WorkingDirectory <- workingDir.FullName
                    ) timeToComplete
    if result <> 0 then
        failwith "Could not minify SVG images in a timely fashion."

let private minifyHtml (workingDir: DirectoryInfo) =
    let result = 
        ExecProcess (fun info ->
                        if isLinux then
                            info.FileName <- "node"
                            info.Arguments <- sprintf "../node_modules/html-minifier/src/htmlminifier.js --html5 --minify-js true --collapse-whitespace --file-ext html --input-dir '%s' --output-dir '%s'" workingDir.FullName workingDir.FullName
                        elif isWindows then
                            info.FileName <- "node.exe"
                            info.Arguments <- sprintf "..\\node_modules\\html-minifier\\src\\htmlminifier.js --html5 --minify-js true --collapse-whitespace --file-ext html --input-dir '%s' --output-dir '%s'" workingDir.FullName workingDir.FullName
                        else
                            failwith "Only linux and windows are supported at this moment in time."
                        info.WorkingDirectory <- workingDir.FullName
                    ) timeToComplete
    if result <> 0 then
        failwith "Could not minify HTML documents in a timely fashion."

Target "CopyStaticContent" (fun () -> 
    !! "src/*.html"
    ++ "src/*.less"
    ++ "src/*.js"
    ++ "src/*.vcf"
    ++ "src/images/*.svg"
    ++ "src/images/*.png"
    ++ "src/fonts/*.eot"
    ++ "src/fonts/*.svg"
    ++ "src/fonts/*.ttf"
    ++ "src/fonts/*.woff"
    ++ "src/fonts/*.woff2"
    |> Seq.iter (fun path -> 
        CopyFileWithSubfolder srcDir.FullName siteDir.FullName path
    )
    
    compileLess siteDir "home"
    compileLess siteDir "services"
    compileLess siteDir "contact"
    compileLess siteDir "blog"
    compileLess siteDir "policy"
    compileLess siteDir "blogpost"
    compileLess siteDir "highlight"
    compileLess siteDir "browsernotsupported"
    
    !! "site/*.less" |> Seq.iter DeleteFile
)

Target "CopyBlogContent" (fun () ->
    //Blog
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

Target "Init" (fun () -> 
    CreateDir siteDir.FullName
)

Target "Build" (fun () -> 
    minifySvg thisDir siteImagesDir
    minifyHtml siteDir
)

"Clean"
==> "Init"
==> "CopyStaticContent"
==> "CopyBlogContent"
==> "Build"

RunTargetOrDefault "Build"