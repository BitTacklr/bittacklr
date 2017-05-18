---
original: https://seabites.wordpress.com/2012/02/11/using-amazon-web-services-to-build-a-simple-event-store/
title: "Using Amazon Web Services to build a simple Event Store"
slug: "using-amazon-web-services-to-build-a-simple-event-store"
date: 2012-02-11
author: Yves Reynhout
publish: false
---
My post on "[Your EventStream is a linked list](http://seabites.wordpress.com/2011/12/07/your-eventstream-is-a-linked-list/ "Your EventStream is a linked list")" might have been somewhat abstract (a more prosaic version can be found [here](http://stackoverflow.com/questions/9083972/is-it-possible-to-make-conditional-inserts-with-azure-table-storage/9085397#9085397 "Prosaic version of Your EventStream is a linked list")). By way of *testing* my *theory*, I set out to apply it using Amazon's Web Services (AWS) stack. I used AWS DynamoDB as the transactional medium that would handle the optimistic concurrency, and AWS S3 to store the changesets.

> Disclaimer: This is just a proof of concept, a way to get started. It's not production code ... obviously :-) I'm assuming you know your way around AWS (how to set up an account, stuff like that) and how the [.NET SDK](http://aws.amazon.com/net/ "Windows & .NET SDK home") works.

The first thing todo is to create the container for the *changesets*. In AWS S3 those things are called *buckets*. This is a one time operation. You might want to make this conditional at the startup of your application/worker role/what-have-you. The AWS S3 API provides mechanisms to query for buckets, so you should be able to pull that off. On the other hand, when creating buckets using the REST API the HTTP PUT verb is used, and we all know that PUT is supposed to be idempotent. You might want to read up on the [PUT Bucket API](http://docs.amazonwebservices.com/AmazonS3/latest/API/RESTBucketPUT.html "AWS S3 PUT Bucket API"). One last thing: bucket names are unique across all of S3. 

```csharp
 //Setting up the Amazon S3 bucket to store changesets in const string ChangesetBucketName = "yourorganization\_yourboundedcontextname\_changesets"; var s3Client = AWSClientFactory.CreateAmazonS3Client(); s3Client.PutBucket( new PutBucketRequest(). WithBucketName(ChangesetBucketName). WithBucketRegion(S3Region.EU)); //Your region might vary 
```

 The same thing needs to happen in AWS DynamoDB. There, the containers are called *tables*. Again, this is a one time, conditional operation. You might wanna [read up](http://docs.amazonwebservices.com/amazondynamodb/latest/developerguide/API_CreateTable.html "AWS DynamoDB CreateTable API docs") on the specifics of table naming and uniqueness. The [throughput provisioning](http://docs.amazonwebservices.com/amazondynamodb/latest/developerguide/ProvisionedThroughputIntro.html "AWS DynamoDB throughput provisioning") is not something you want to be static. Monitor the load on your system (they have notification events/alerts for that), both in terms of number of reads/writes and bytes used for storage, and use that information to tune the table throughput settings. 

```csharp
 //Setting up the Amazon DynamoDB table to store aggregates in const string AggregateTableName = "yourboundedcontextname\_aggregates"; const string AggregateIdName = "aggregate-id"; //At the time of writing, DynamoDBClient didn't make it yet //into the AWSClientFactory. var dynamoDbClient = new AmazonDynamoDBClient(); dynamoDbClient.CreateTable( new CreateTableRequest(). WithTableName(AggregateTableName). WithKeySchema( new KeySchema(). WithHashKeyElement( new KeySchemaElement(). WithAttributeName(AggregateIdName). WithAttributeType("S") ). WithRangeKeyElement(null) ). WithProvisionedThroughput( new ProvisionedThroughput(). WithReadCapacityUnits(300). WithWriteCapacityUnits(500) ) ); 
```

 Now that we've set up the *infrastructure*, let's tackle the scenario of storing a changeset. The flow is pretty simple. First we try to store the changeset in AWS S3 as an object in the configured bucket. 

```csharp
 //An internal representation of the changeset public class ChangesetDocument { public const long InitialValue = 0; //Exposing internal state here to //simplify the example. public Guid AggregateId { get; set; } public long AggregateVersion { get; set; } public Guid ChangesetId { get; set; } public Guid? ParentChangesetId { get; set; } public byte\[\] Content { get; set; } public Stream GetContentStream() { return new MemoryStream(Content, writable: false); } } //Assuming there's a changeset document we want to store, //going by the variable name 'document'. const string AggregateIdMetaName = "x-amz-meta-aggregate-id"; const string AggregateVersionMetaName = "x-amz-meta-aggregate-version"; const string ChangesetIdMetaName = "x-amz-meta-changeset-id"; const string ParentChangesetIdMetaName = "x-amz-meta-parent-changeset-id"; var putObjectRequest = new PutObjectRequest(). WithBucketName(ChangesetBucketName). WithGenerateChecksum(true). WithKey(document.ChangesetId.ToString()). WithMetaData(ChangesetIdMetaName, document.ChangesetId.ToString()). WithMetaData(AggregateIdMetaName, document.AggregateId.ToString()). WithMetaData(AggregateVersionMetaName, Convert.ToString(document.AggregateVersion)); if (document.ParentChangesetId.HasValue) { putObjectRequest.WithMetaData(ParentChangesetIdMetaName, document.ParentChangesetId.Value.ToString()); } putObjectRequest.WithInputStream(document.GetContentStream()); s3Client.PutObject(putObjectRequest); 
```

 If that goes well, we try to create an item in the configured AWS DynamoDB table. The expected values below are the AWS DynamoDB way of doing optimistic concurrency checking. They cater for both the initial (competing inserters) and update (competing updaters) concurrency.

> In this example I'm using an incrementing version number to do that. Strictly speaking, I could be using the changeset identifier for that, but an incrementing version number is easier to relate to when you come from an ORM/database background.



```csharp
 const string AggregateVersionName = "aggregate-version"; const string ChangesetIdName = "changeset-id"; public static class ExtensionsForChangesetDocument { public static KeyValuePair&lt;string, ExpectedAttributeValue&gt;\[\] ToExpectedValues(this ChangesetDocument document) { var dictionary = new Dictionary&lt;string, ExpectedAttributeValue&gt;(); if(document.AggregateVersion == ChangesetDocument.InitialValue) { //Make sure we're the first to create the aggregate dictionary.Add(AggregateIdName, new ExpectedAttributeValue(). WithExists(false) ); } else { //Make sure nobody changed the aggregate behind our back dictionary.Add(AggregateIdName, new ExpectedAttributeValue(). WithExists(true). WithValue( new AttributeValue(). WithS(document.AggregateId.ToString())) ); dictionary.Add(AggregateVersionName, new ExpectedAttributeValue(). WithValue( new AttributeValue(). WithN(Convert.ToString(document.AggregateVersion - 1))) ); } return dictionary.ToArray(); } public static KeyValuePair&lt;string, AttributeValue&gt;\[\] ToItemValues(this ChangesetDocument document) { //The relevant values to store in the item. var dictionary = new Dictionary&lt;string, AttributeValue&gt; { {AggregateIdName, new AttributeValue().WithS(document.AggregateId.ToString())}, {AggregateVersionName, new AttributeValue().WithN(Convert.ToString(document.AggregateVersion))}, {ChangesetIdName, new AttributeValue().WithS(document.ChangesetId.ToString())}, }; return dictionary.ToArray(); } } dynamoDbClient.PutItem( new PutItemRequest(). WithTableName(AggregateTableName). WithExpected(document.ToExpectedValues()). WithItem(document.ToItemValues()) ); 
```

 If the PutItem operation throws an exception indicating that the "ConditionalCheckFailed", we know there was some form of optimistic concurrency. In such a case, it's best to repeat the entire operation.

> I've left duplicate command processing elimination as an exercise for the reader ;-)

The only thing left todo is showing the reverse operation, reading. First we need to fetch the pointer to the last stored and approved changeset identifier. I hope you do realize that a consistent read is not strictly necessary, since the likelihood of concurrency should be low. 

```csharp
 var getItemResponse = dynamoDbClient.GetItem( new GetItemRequest(). WithTableName(AggregateTableName). WithKey( new Key(). WithHashKeyElement( new AttributeValue(). WithS(aggregateId.ToString())). WithRangeKeyElement(null)). WithConsistentRead(false). WithAttributesToGet(ChangesetIdName) ); 
```

 Now that we've bootstrapped the reading process, we keep reading each changeset, until there's no more changeset to read. Each approved changeset contains metadata that points to the previous approved changeset. It's the responsibility of the calling code todo something useful with the read changesets (e.g. deserialize the content and replay each embedded event into the corresponding aggregate). 

```csharp
 var changesetId = new Guid?(new Guid(getItemResponse.GetItemResult.Item\[ChangesetIdName\].S)); while(changesetId.HasValue) { var getObjectResponse = s3Client.GetObject( new GetObjectRequest(). WithBucketName(ChangesetBucketName). WithKey(changesetId.Value.ToString())); var document = new ChangesetDocument { AggregateId = new Guid(getObjectResponse.Metadata\[AggregateIdMetaName\]), AggregateVersion = Convert.ToInt64(getObjectResponse.Metadata\[AggregateVersionMetaName\]), ChangesetId = new Guid(getObjectResponse.Metadata\[ChangesetIdMetaName\]), Content = getObjectResponse.ResponseStream.ToByteArray() }; var parentChangesetIdAsString = getObjectResponse.Metadata\[ParentChangesetIdMetaName\]; if(parentChangesetIdAsString != null) { document.ParentChangesetId = new Guid(parentChangesetIdAsString); } changesetId = document.ParentChangesetId; yield return document; } 
```

 And that's basically all there's to it. In reality, you'll need a lot more metadata and error handling to make this a success. I should point out that the performance of these services consumed on premise was abominal. I'm assuming (hoping) that if you consume them from an AWS EC2 instance, performance will be much better.

Conclusion
----------

A clever reader will notice that this technique can easily be transposed to other document/data stores. The specifics will vary, but the theme will be the same. The reason for applying this technique has mainly todo with the inherent constraints of certain datastores. The transactional ones - especially in cloudy environments - are limited with regard to the number of bytes they can store, which makes them less desirable for storing payloads in (prediction of payload size can be hard at times). There are easy ways to side-step concurrency problems such as serializing all aggregate access to one worker/queue, but that's another discussion.
