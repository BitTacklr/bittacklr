---
original: https://seabites.wordpress.com/2012/02/16/using-windows-azure-to-build-a-simple-eventstore/
title: "Using Windows Azure to build a simple EventStore"
slug: "using-windows-azure-to-build-a-simple-eventstore"
date: 2012-02-16
author: Yves Reynhout
publish: true
---
Continuing my journey, I figured it shouldn't be that hard to apply [“Your EventStream is a linked list”](http://seabites.wordpress.com/2011/12/07/your-eventstream-is-a-linked-list/ "Your EventStream is a linked list") using [Windows Azure Storage Services](http://www.windowsazure.com "Windows Azure").

> Disclaimer: Again, this is just a proof of concept, a way to get started, not production code … obviously. I’m assuming you know your way around Windows Azure (how to set up an account, stuff like that) and how the [.NET SDK](http://www.microsoft.com/download/en/details.aspx?id=28045 "Windows Azure .NET SDK") works.

Just like with [Amazon Web Services](http://aws.amazon.com "Amazon Web Services"), the first thing to create are the containers that will harbor the changesets and aggregate heads. Unlike the [AWS example](http://seabites.wordpress.com/2012/02/11/using-amazon-web-services-to-build-a-simple-event-store/ "Using Amazon Web Services to build a simple Event Store"), I've chosen to store both using the same service, [Windows Azure Blob Storage](http://www.windowsazure.com/home/tour/storage/ "Windows Azure Blob Storage"). Why? Because it offers optimistic concurrency by leveraging [ETags](http://en.wikipedia.org/wiki/HTTP_ETag "HTTP ETag"). The only unfortunate side-effect is that it seems to force me to break CQS, but I can live with that for now. The Windows Azure Blob Storage provides convenience methods to only create the containers when they don't exist. 

```csharp
 //Setting up the Windows Azure Blob Storage container to store changesets and aggregate heads const string PrimaryAccessKey = "your-key-here-for-testing-purposes-only-ofcourse-;-)"; const string AccountName = "youraccountname"; const string ChangesetContainerName = "changesets"; const string AggregateContainerName = "aggregates"; var storageAccount = new CloudStorageAccount( new StorageCredentialsAccountAndKey( AccountName, PrimaryAccessKey), false); var blobClient = storageAccount.CreateCloudBlobClient(); var changesetContainer = blobClient.GetContainerReference(ChangesetContainerName); changesetContainer.CreateIfNotExist(); var aggregateContainer = blobClient.GetContainerReference(AggregateContainerName); aggregateContainer.CreateIfNotExist(); 
```

 Now that we’ve set up the infrastructure, let’s tackle the scenario of storing a changeset. The flow is pretty simple. First we try to store the changeset in the changeset container as a blob. 

```csharp
 public class ChangesetDocument { public const long InitialVersion = 0; //Exposing internals to make the sample easier public Guid ChangesetId { get; set; } public Guid? ParentChangesetId { get; set; } public Guid AggregateId { get; set; } public long AggregateVersion { get; set; } public string AggregateETag { get; set; } public byte\[\] Content { get; set; } } //Assuming there's a changeset document we want to store, //going by the variable name 'document'. var changesetBlob = changesetContainer.GetBlobReference(document.ChangesetId.ToString()); var changesetUploadOptions = new BlobRequestOptions { AccessCondition = AccessCondition.None, BlobListingDetails = BlobListingDetails.None, CopySourceAccessCondition = AccessCondition.None, DeleteSnapshotsOption = DeleteSnapshotsOption.None, RetryPolicy = RetryPolicies.NoRetry(), Timeout = TimeSpan.FromSeconds(90), UseFlatBlobListing = false }; changesetBlob.UploadByteArray(document.Content, changesetUploadOptions); const string AggregateIdMetaName = "aggregateid"; const string AggregateVersionMetaName = "aggregateversion"; const string ChangesetIdMetaName = "changesetid"; const string ParentChangesetIdMetaName = "parentchangesetid"; //Set the meta-data of the changeset //Notice how this doesn't need to be transactional changesetBlob.Metadata\[AggregateIdMetaName\] = document.AggregateId.ToString(); changesetBlob.Metadata\[AggregateVersionMetaName\] = document.AggregateVersion.ToString(); changesetBlob.Metadata\[ChangesetIdMetaName\] = document.ChangesetId.ToString(); if(document.ParentChangesetId.HasValue) changesetBlob.Metadata\[ParentChangesetIdMetaName\] = document.ParentChangesetId.Value.ToString(); changesetBlob.SetMetadata(); 
```

 If that goes well, we try to upsert the head of the aggregate to get it to point to this changeset. Below, we're using the ETag of the aggregate head blob as a way of doing optimistic concurrency checking. It caters for both the initial (competing inserters) and update (competing updaters) concurrency. 

```csharp
 public static class ExtensionsForChangeDocument { public static AccessCondition ToAccessCondition(this ChangesetDocument document) { if(document.AggregateVersion == ChangesetDocument.InitialVersion) { return AccessCondition.IfNoneMatch("\*"); } return AccessCondition.IfMatch(document.AggregateETag); } } //Upsert the aggregate var aggregateBlob = aggregateContainer.GetBlobReference(document.AggregateId.ToString()); var aggregateUploadOptions = new BlobRequestOptions { AccessCondition = document.ToAccessCondition(), BlobListingDetails = BlobListingDetails.None, CopySourceAccessCondition = AccessCondition.None, DeleteSnapshotsOption = DeleteSnapshotsOption.None, RetryPolicy = RetryPolicies.NoRetry(), Timeout = TimeSpan.FromSeconds(90), UseFlatBlobListing = false }; aggregateBlob.UploadByteArray(document.ChangesetId.ToByteArray(), aggregateUploadOptions); //Here's where we are breaking CQS if we'd like to cache the aggregate. //This won't be a problem if we're re-reading the aggregate upon each behavior. var eTag = aggregateBlob.Properties.ETag; 
```

 If the UploadByteArray operation throws a StorageClientException indicating that “The condition specified using HTTP conditional header(s) is not met.”, we know there was some form of optimistic concurrency. In such a case, it’s best to repeat the entire operation.
Now that we've dealt with writing, let's take a look at reading. First we need to fetch the pointer to the last stored and approved changeset identifier. 

```csharp
 var aggregateBlob = aggregateContainer.GetBlobReference(aggregateId.ToString()); var aggregateDownloadOptions = new BlobRequestOptions { AccessCondition = AccessCondition.None, BlobListingDetails = BlobListingDetails.None, CopySourceAccessCondition = AccessCondition.None, DeleteSnapshotsOption = DeleteSnapshotsOption.None, RetryPolicy = RetryPolicies.NoRetry(), Timeout = TimeSpan.FromSeconds(90), UseFlatBlobListing = false }; var changesetId = new Guid?(new Guid(aggregateBlob.DownloadByteArray(aggregateDownloadOptions))); var eTag = aggregateBlob.Properties.ETag; 
```

 Now that we’ve bootstrapped the reading process, we keep reading each changeset, until there’s no more changeset to read. Each approved changeset contains metadata that points to the previous approved changeset. It’s the responsibility of the calling code todo something useful with the read changesets (e.g. deserialize the content and replay each embedded event into the corresponding aggregate). 

```csharp
 while(changesetId.HasValue) { var changesetBlob = changesetContainer.GetBlobReference(changesetId.Value.ToString()); var changesetDownloadOptions = new BlobRequestOptions { AccessCondition = AccessCondition.None, BlobListingDetails = BlobListingDetails.None, CopySourceAccessCondition = AccessCondition.None, DeleteSnapshotsOption = DeleteSnapshotsOption.None, RetryPolicy = RetryPolicies.NoRetry(), Timeout = TimeSpan.FromSeconds(90), UseFlatBlobListing = false }; var content = changesetBlob.DownloadByteArray(changesetDownloadOptions); changesetBlob.FetchAttributes(); var document = new ChangesetDocument { AggregateETag = eTag, AggregateId = new Guid(changesetBlob.Metadata\[AggregateIdMetaName\]), AggregateVersion = Convert.ToInt64(changesetBlob.Metadata\[AggregateVersionMetaName\]), ChangesetId = new Guid(changesetBlob.Metadata\[ChangesetIdMetaName\]), Content = content, }; if (changesetBlob.Metadata\[ParentChangesetIdMetaName\] != null) document.ParentChangesetId = new Guid(changesetBlob.Metadata\[ParentChangesetIdMetaName\]); yield return document; changesetId = document.ParentChangesetId; } 
```

 The only \*weird\* thing with this code is that I'm propagating the aggregate's head ETag using the changesets. It's a modeling issue I'll have to revisit ;-).
But that’s basically all there’s to it. In reality, you’ll need a lot more metadata and error handling to make this a success. I should point out that the performance of this service consumed on premise was better than what I experienced with AWS.

Conclusion
==========

Using Windows Azure Storage Services is not so different from the Amazon Web Services in this case. However, this overall technique suffers from a few drawbacks. As mentioned before, upon concurrency, you might be wasting some storage space. Another drawback is the fact that you need to read all changesets that make up the history of an aggregate (or eventsource if want to decouple it from DDD terminology) before being able to apply the first event. There's ways around this, such as storing all the changeset identifiers in the aggregate head if the total number of behaviors in an aggregate is low on average. You could even partition the aggregate head into multiple documents using nothing but its version number or count of applied behaviors to partition, but that's the subject of a future exploration. I apologize if this post is a somewhat copy-paste of its AWS counterpart, but given the goal and similarities that was to be expected :-).
