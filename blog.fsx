#I "packages/FAKE/tools"
#r "FakeLib.dll" // include Fake lib
#r "packages/YamlDotNet/lib/net35/YamlDotNet.dll"
#r "packages/AngleSharp/lib/net45/AngleSharp.dll"
#load "atomfeed.fsx"

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Globalization
open System.Linq
open System.Threading
open System.Security

open YamlDotNet.RepresentationModel
open AngleSharp
open AngleSharp.Dom
open AngleSharp.Dom.Html
open AngleSharp.Extensions
open AngleSharp.Html
open AngleSharp.Parser.Html

open Atomfeed

open Fake
/// Settings
let private timeToRenderBlogPost = Convert.ToInt32(TimeSpan.FromSeconds(5.0).TotalMilliseconds)
let private blogPostAssetPatterns = 
    [| 
        "*.svg"
        "*.png"
        "*.jpg" 
    |]
let private englishCulture = CultureInfo("en-US")

/// Model
type private BlogPostMetadata =
    {
        Title: string option
        Slug: string option
        Date: DateTime option
        Author: string
        Published: bool
        Tags: string []
    }
    with static member empty = { Title = None; Slug = None; Date = None; Author = "Yves Reynhout"; Published = false; Tags = Array.empty }

type private BlogPost =
    {
        BaseDirectory: DirectoryInfo
        File: FileInfo
        Metadata: BlogPostMetadata
    }
    member this.Date 
        with get() = 
            match this.Metadata.Date with
            | Some date -> date
            | None -> this.File.CreationTimeUtc

    member this.Id 
        with get() = 
            this.File.FullName.Substring(this.BaseDirectory.FullName.Length + 1).Replace(Path.DirectorySeparatorChar, '-').Replace(Path.AltDirectorySeparatorChar, '-')
    member this.Slug 
        with get() =
            match this.Metadata.Slug with
            | Some slug -> slug
            | None -> Path.GetFileNameWithoutExtension(this.File.Name)

    member this.ResolveTargetFile (targetDir: DirectoryInfo) =
        let filename = sprintf "%s.html" this.Slug
        let relativePath =
            match this.Metadata.Date with
            | Some date ->
                sprintf "%04d/%s/%02d" date.Year (englishCulture.DateTimeFormat.GetMonthName(date.Month).ToLowerInvariant()) date.Day
            | None ->
                this.File.DirectoryName.Substring(this.BaseDirectory.FullName.Length + 1)
        FileInfo(Path.Combine(targetDir.FullName, Path.Combine(relativePath, filename)))
    member this.ResolveTargetUrl (baseUrl: string) =
        let filename = sprintf "%s.html" this.Slug
        let relativePath =
            match this.Metadata.Date with
            | Some date ->
                sprintf "%04d/%s/%02d" date.Year (englishCulture.DateTimeFormat.GetMonthName(date.Month).ToLowerInvariant()) date.Day
            | None ->
                this.File.DirectoryName.Substring(this.BaseDirectory.FullName.Length + 1)
        sprintf "%s/%s/%s" baseUrl relativePath filename

type BlogSettings = 
    { 
        SiteUrl: string
        SiteDir: DirectoryInfo
        SiteBlogDir: DirectoryInfo
        BlogBaseUrl: string
        SrcDir: DirectoryInfo
        BlogPostsDir: DirectoryInfo
        TemplatesDir: DirectoryInfo
    }

type private BlogPostMetadataVisitor() =
    inherit YamlVisitorBase()
    let mutable metadata = BlogPostMetadata.empty
    override this.VisitPair (key: YamlNode, value: YamlNode) =
        match key with
        | :? YamlScalarNode as k ->
            match k.Value with
            | "title" -> 
                match value with
                | :? YamlScalarNode as v ->
                    metadata <- { metadata with Title = Some(v.Value) }
                | _ -> ()
            | "slug" -> 
                match value with
                | :? YamlScalarNode as v ->
                    metadata <- { metadata with Slug = Some(v.Value) }
                | _ -> ()
            | "date" -> 
                match value with
                | :? YamlScalarNode as v ->
                    let (parsed, date) = DateTimeOffset.TryParse(v.Value)
                    if parsed then metadata <- { metadata with Date = Some(date.Date) }
                | _ -> ()
            | "author" ->
                match value with
                | :? YamlScalarNode as v ->
                    metadata <- { metadata with Author = v.Value }
                | _ -> ()
            | "publish" ->
                match value with
                | :? YamlScalarNode as v ->
                    metadata <- { metadata with Published = Boolean.Parse(v.Value) }
                | _ -> ()
            | "tags" ->
                match value with
                | :? YamlSequenceNode as v ->
                    let tags =
                        v.Children 
                        |> Seq.choose (fun child -> 
                            match child with
                            | :? YamlScalarNode as cv -> Some(cv)
                            | _ -> None
                        )
                        |> Seq.map (fun scalar -> scalar.Value)
                        |> Seq.toArray
                    
                    metadata <- { metadata with Tags = tags }
                | _ -> ()
            | _ -> ()
        | _ -> ()
        base.VisitPair(key, value) 
    member this.Metadata with get() = metadata

/// Functions
let private readBlogPostMetadata (file: FileInfo) =
    trace (sprintf "Reading metadata of blogpost %s" file.FullName)
    let lines = File.ReadAllLines(file.FullName)
    let frontmatter = lines |> Array.skip 1 |> Array.takeWhile (fun line -> not(line.StartsWith("---") || line.StartsWith("..."))) |> String.concat Environment.NewLine
    using(new StringReader(frontmatter)) (fun reader ->
        let stream = YamlStream()
        stream.Load(reader)
        match stream.Documents.Count with
        | 1 -> 
            let visitor = BlogPostMetadataVisitor()
            stream.Accept(visitor)
            visitor.Metadata
        | _ -> 
            BlogPostMetadata.empty
    )

let private renderBlogPost (template: FileInfo) (input: FileInfo) (output: FileInfo) =
    trace (sprintf "Rendering blogpost %s" input.FullName)
    //let info = ProcessStartInfo("pandoc", sprintf "--read=markdown_github+yaml_metadata_block --write=html5 --template='%s' --output='%s' '%s'" template.FullName output.FullName input.FullName)
    let info = ProcessStartInfo("pandoc", sprintf "--read=markdown_github+yaml_metadata_block --write=html5 --standalone --output='%s' '%s'" output.FullName input.FullName)
    use pandoc = Process.Start(info)
    if not(pandoc.WaitForExit(timeToRenderBlogPost)) then failwith (sprintf "Rendering %s using pandoc took longer than %dms." input.FullName timeToRenderBlogPost)
    if pandoc.ExitCode <> 0 then failwith (sprintf "Rendering %s using pandoc failed with exit code %d." input.FullName pandoc.ExitCode)

let private generateBlogPosts (blogPosts: BlogPost []) (blogPostTemplate: FileInfo) (siteDir: DirectoryInfo) =
    trace "Generating blogposts"
    blogPosts
    |> Array.iter (fun blogPost -> 
        let targetFile = blogPost.ResolveTargetFile siteDir
        if not(targetFile.Directory.Exists) then targetFile.Directory.Create()

        renderBlogPost blogPostTemplate blogPost.File targetFile

        blogPostAssetPatterns
        |> Array.iter (fun blogPostAssetPattern ->
            blogPost.File.Directory.GetFiles(blogPostAssetPattern, SearchOption.AllDirectories)
            |> Seq.iter (fun blogPostAssetFile -> 
                File.Copy(blogPostAssetFile.FullName, Path.Combine(targetFile.Directory.FullName, blogPostAssetFile.Name))
            )
        )
    )

let private generateAtomFeed (blogPosts: BlogPost []) (settings: BlogSettings) =
    trace "Generating atom feed"
    let latest = blogPosts |> Array.map (fun blogPost -> blogPost.Date) |> Array.max
    let feed = 
        { 
            SiteUrl = settings.SiteUrl
            BaseUrl = settings.BlogBaseUrl
            Date = sprintf "%sT00:00:00Z" (latest.ToString("yyyy-MM-dd"))
            Entries = 
                blogPosts 
                |> Array.map (fun blogPost ->
                    let targetFile = blogPost.ResolveTargetFile settings.SiteBlogDir
                    {
                        Id = blogPost.Id
                        Title = Option.defaultValue (Path.GetFileNameWithoutExtension(blogPost.File.Name)) blogPost.Metadata.Title
                        RelativeUrl = targetFile.FullName.Substring(settings.SiteBlogDir.FullName.Length + 1)
                        Date = sprintf "%sT00:00:00Z" (blogPost.Date.ToString("yyyy-MM-dd"))
                        Content = SecurityElement.Escape(File.ReadAllText(targetFile.FullName))
                    }
                )
        }
    Atomfeed.generate feed settings.SiteDir

let private generateBlogPage (blogPosts: BlogPost []) (settings: BlogSettings) =
    trace "Generating blog page"
    let targetFile = FileInfo(Path.Combine(settings.SiteDir.FullName, "blog.html"))
    let blogFile = FileInfo(Path.Combine(settings.TemplatesDir.FullName, "blog.html"))
    let parser = HtmlParser()
    using(blogFile.OpenRead()) (fun inputStream ->
        use html = parser.Parse(inputStream)
        let smallContent = html.GetElementById("smallcontent")
        let yearContentTemplateNode = smallContent.FirstElementChild
        let smallBitIcon = smallContent.LastElementChild
        let yearContentNodes =
            blogPosts
            |> Seq.sortByDescending (fun blogPost -> blogPost.Date)
            |> Seq.groupBy (fun blogPost -> blogPost.Date.Year)
            |> Seq.map (fun (year, posts) -> 
                let yearContentNode = yearContentTemplateNode.Clone(true) :?> IElement
                let yearNode = 
                    yearContentNode.Descendents().OfType<IElement>()
                    |> Seq.find (fun (node: IElement) -> 
                        node.Attributes |> Seq.exists (fun attribute -> attribute.Name = "data-id" && attribute.Value = "year")
                    )
                yearNode.TextContent <- year.ToString()
                let postTemplateNode = 
                    yearContentNode.
                        Descendents().
                        OfType<IElement>()
                        |> Seq.find (fun (node: IElement) -> 
                            node.Attributes |> Seq.exists (fun attribute -> attribute.Name = "data-id" && attribute.Value = "post")
                        )
                posts
                |> Seq.iter (fun post ->
                    let url = post.ResolveTargetUrl settings.BlogBaseUrl
                    let postNode = postTemplateNode.Clone(true) :?> IElement
                    postNode.FirstElementChild.SetAttribute("href", url)
                    postNode.FirstElementChild.TextContent <- Option.defaultValue "pff" post.Metadata.Title
                    postTemplateNode.Parent.AppendElement(postNode) |> ignore
                )
                postTemplateNode.Remove()
                yearContentNode :> INode
            )
            |> Seq.toArray
        smallBitIcon.Before(yearContentNodes)
        yearContentTemplateNode.Remove()
        using(targetFile.OpenWrite()) (fun outputStream ->
            using(new StreamWriter(outputStream)) (fun writer ->
                html.ToHtml(writer, PrettyMarkupFormatter())
                writer.Flush()
            )
        )
    )

let generate (settings: BlogSettings) =
    // Generate blogposts
    let allBlogPostFiles = settings.BlogPostsDir.GetFiles("*.md", SearchOption.AllDirectories)

    let blogPosts =
        allBlogPostFiles
        |> Array.map (fun (file: FileInfo) -> 
            { 
                BaseDirectory = settings.BlogPostsDir
                File = file
                Metadata = readBlogPostMetadata file
            }
        )
        |> Array.filter (fun blogPost -> blogPost.Metadata.Published)

    let blogPostTemplate = FileInfo(Path.Combine(settings.TemplatesDir.FullName, "blogpost.html"))
    generateBlogPosts blogPosts blogPostTemplate settings.SiteBlogDir
    generateAtomFeed blogPosts settings
    generateBlogPage blogPosts settings