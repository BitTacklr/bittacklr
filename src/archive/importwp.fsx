open System
open System.Diagnostics
open System.Linq
open System.IO
open System.Net
open System.Text.RegularExpressions
open System.Xml
open System.Xml.XPath

let srcDir = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "src"))
let blogPostsDir = DirectoryInfo(Path.Combine(srcDir.FullName, "posts"))

let wp = Path.Combine(__SOURCE_DIRECTORY__, "wp.xml")

let timeToConvert = Convert.ToInt32(TimeSpan.FromSeconds(5.0).TotalMilliseconds)

let convert content =
    let input = FileInfo(Path.GetTempFileName())
    let output = FileInfo(Path.GetTempFileName())
    File.WriteAllText(input.FullName, content)
    let info = ProcessStartInfo("pandoc", sprintf "--read=html --write=markdown_github+backtick_code_blocks+blank_before_blockquote+blank_before_header+hard_line_breaks --wrap=preserve --output='%s' '%s'" output.FullName input.FullName)
    use pandoc = Process.Start(info)
    if not(pandoc.WaitForExit(timeToConvert)) then failwith (sprintf "Converting %s using pandoc took longer than %dms." input.FullName timeToConvert)
    if pandoc.ExitCode <> 0 then failwith (sprintf "Converting %s using pandoc failed with exit code %d." input.FullName pandoc.ExitCode)
    let converted = File.ReadAllText(output.FullName)
    input.Delete()
    output.Delete()
    converted

let imageTitlePattern = @"\[<img\s+(?<pairsbefore>(?<key>[a-zA-Z0-9]+)=""(?<value>[-_\s\.\:/a-zA-Z0-9]+)""\s+)*title=""(?<title>[\s-_/\\a-zA-Z0-9]+)""\s+(?<pairsafter>(?<key>[a-zA-Z0-9]+)=""(?<value>[-_\s\.\:/a-zA-Z0-9]+)""\s+)*\s*/>\]"
let imageUrlPattern = "\\(http://seabites.files.wordpress.com/.*/(?<image>[-_a-zA-Z0-9]*\\.(png|jpg|gif))\\)"
let imageTitleExpression = Regex(imageTitlePattern, RegexOptions.IgnoreCase)
let imageUrlExpression = Regex(imageUrlPattern, RegexOptions.IgnoreCase)
let markdownify (content: string) =
    let converted = convert content
    imageTitleExpression
        .Replace(
            imageUrlExpression.Replace(converted, "(${image})"),
            "![${title}]")
        .Replace("\\[sourcecode language=\"csharp\"\\]", Environment.NewLine + Environment.NewLine + "```csharp" + Environment.NewLine)
        .Replace("\\[/sourcecode\\]", Environment.NewLine + "```" + Environment.NewLine + Environment.NewLine)
        .Replace("\\[code language=\"csharp\"\\]", Environment.NewLine + Environment.NewLine + "```csharp" + Environment.NewLine)
        .Replace("\\[/code\\]", Environment.NewLine + "```" + Environment.NewLine + Environment.NewLine)

let imageNewUrlPattern = "\\((?<image>[-_a-zA-Z0-9]+\\.(png|jpg|gif))\\)"
let imageNewUrlExpression = Regex(imageNewUrlPattern, RegexOptions.IgnoreCase)
let import =
    let wc = new WebClient()
    let table = NameTable()
    let ns = XmlNamespaceManager(table)
    ns.AddNamespace("content", "http://purl.org/rss/1.0/modules/content/")
    ns.AddNamespace("wfw", "http://wellformedweb.org/CommentAPI/")
    ns.AddNamespace("dc", "http://purl.org/dc/elements/1.1/")
    ns.AddNamespace("wp", "http://wordpress.org/export/1.2/")
    ns.AddNamespace("excerpt", "http://wordpress.org/export/1.2/excerpt/")
    printfn "Loading xml document"
    let document = XmlDocument()
    document.LoadXml(File.ReadAllText(wp))
    printfn "Importing posts"
    let items = document.SelectNodes("/rss/channel/item", ns)
    items.Cast<XmlNode>()
    |> Seq.iter 
        (fun item ->
            let title_node = item.SelectSingleNode("title", ns)
            let link_node = item.SelectSingleNode("link", ns)
            let postid_node = item.SelectSingleNode("wp:post_id", ns)
            let postdate_node = item.SelectSingleNode("wp:post_date", ns)
            let posttype_node = item.SelectSingleNode("wp:post_type", ns)
            let attachment_url_node = item.SelectSingleNode("wp:attachment_url", ns)
            let status_node = item.SelectSingleNode("wp:status", ns)
            let tag_node = item.SelectSingleNode("category[@domain='post_tag']/@nicename", ns)
            let content_node = item.SelectSingleNode("content:encoded", ns)

            if not(isNull(posttype_node)) && not(isNull(postid_node)) && not(isNull(postdate_node)) then
                match posttype_node.InnerText with
                | "post" ->
                    if not(isNull(status_node)) then
                        match status_node.InnerText with
                        | "publish" -> 
                            let postDate = DateTimeOffset.Parse(postdate_node.InnerText.Replace(' ', 'T'))
                            let postId = Int32.Parse(postid_node.InnerText)
                            let blogPostPath = sprintf "%04d-%03d" (postDate.Year) postId
                            let blogPostDir = DirectoryInfo(Path.Combine(blogPostsDir.FullName, blogPostPath))
                            if not(blogPostDir.Exists) then blogPostDir.Create()
                            let blogPostMarkdownFile = FileInfo(Path.Combine(blogPostDir.FullName, "post.md"))
                            printfn "Handling %s" blogPostMarkdownFile.FullName
                            if blogPostMarkdownFile.Exists then blogPostMarkdownFile.Delete()
                            using(blogPostMarkdownFile.OpenWrite()) (fun stream ->
                                using(new StreamWriter(stream)) (fun writer ->
                                    let link = link_node.InnerText.Substring(0, link_node.InnerText.Length - 1)
                                    let slug = link.Substring(link.LastIndexOf("/") + 1)
                                    //front matter
                                    writer.WriteLine("---")
                                    writer.WriteLine("original: {0}", link_node.InnerText)
                                    writer.WriteLine("title: \"{0}\"", title_node.InnerText.Replace("\"", "\\\""))
                                    writer.WriteLine("slug: \"{0}\"", slug)
                                    writer.WriteLine("date: {0}", (sprintf "%04d-%02d-%02d" postDate.Year postDate.Month postDate.Day))
                                    writer.WriteLine("author: Yves Reynhout")
                                    if not(isNull(tag_node)) then 
                                        writer.WriteLine("tags: [{0}]", tag_node.Value)
                                    writer.WriteLine("publish: true")
                                    writer.WriteLine("---")
                                    //content
                                    let markdown = markdownify (content_node.ChildNodes.Item(0).InnerText)
                                    writer.Write(markdown)
                                    writer.Flush()
                                )
                            )
                        | _ -> ()
                | "attachment" -> 
                    let url = attachment_url_node.InnerText
                    let name = url.Substring(url.LastIndexOf('/') + 1)
                    let targetFile = FileInfo(Path.Combine(blogPostsDir.FullName, name))
                    if targetFile.Exists then targetFile.Delete()
                    printfn "Handling %s" targetFile.FullName
                    wc.DownloadFile(url, targetFile.FullName)
                | _ -> () //ignore pages
        )
    printfn "Matching images to posts"
    blogPostsDir.EnumerateFiles("*.md", SearchOption.AllDirectories)
    |> Seq.iter (fun postFile ->
        printfn "Handling %s" postFile.FullName
        let content = File.ReadAllText(postFile.FullName)
        let matches = imageNewUrlExpression.Matches(content)
        matches.Cast<Match>()
        |> Seq.iter (fun (matching: Match) ->
            let group = matching.Groups.Item("image")
            printfn "Matched %s" group.Value
            let imageFile = FileInfo(Path.Combine(blogPostsDir.FullName, group.Value))
            if imageFile.Exists then
                printfn "Copying %s" imageFile.FullName
                imageFile.CopyTo(Path.Combine(postFile.Directory.FullName, imageFile.Name)) |> ignore
        )
    )