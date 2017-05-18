---
original: https://seabites.wordpress.com/2011/07/27/eventbuilders/
title: "EventBuilders"
slug: "eventbuilders"
date: 2011-07-27
author: Yves Reynhout
publish: false
---
Some people have asked me to clarify what I've been saying over on [twitter](http://twitter.com/yreynhout "Yves Reynhout") about using event builders. This post is an attempt at showing how I've been using them.

> This is just one way of creating fluent builders for your events. If you search the net, you'll find many more (start [here](http://randypatterson.com/index.php/2007/09/26/how-to-design-a-fluent-interface/ "Fluent Interface Design")).

Let's dive in. I'm assuming there's an event in place (called BeganTemplateRollout in this example) along with some minor infrastructure that tells me the class I'm dealing with is indeed an event. 

```csharp
 //Could be your own, the one from your favourite messaging framework, //or none at all public interface IMessage {} public interface IEvent : IMessage {} public class BeganTemplateRolloutEvent : IEvent { public readonly Guid Id; public readonly long Version; public readonly Events.RolloutPeriod PeriodToRollout; public BeganTemplateRolloutEvent(Guid id, long version, Events.RolloutPeriod periodToRollout) { Id = id; Version = version; PeriodToRollout = periodToRollout; } } public class RolloutPeriod { //i.e. Events.RolloutPeriod, //the event representation of a value object public readonly DateTime StartDate; public readonly DateTime EndDate; public RolloutPeriod(DateTime startDate, DateTime endDate) { StartDate = startDate; EndDate = endDate; } } 
```

 So, given that event, what does its builder look like? 

```csharp
 public interface IEventBuilder&lt;in TEvent&gt; where TEvent : IEvent { TEvent Build(Guid id, long version); } public class BeganTemplateRolloutEventBuilder : IEventBuilder&lt;BeganTemplateRolloutEvent&gt; { Events.RolloutPeriod \_periodToRollout; public BeganTemplateRolloutEventBuilder ForPeriod(RolloutPeriodBuilder builder) { //Notice the builder? Makes it more fluent, that's all. \_periodToRollout= builder.Build(); return this; } public BeganTemplateRolloutEvent Build(Guid id, long version) { return new BeganTemplateRolloutEvent(id, version, \_periodToRollout); } } public class RolloutPeriodBuilder { DateTime \_startsOn; DateTime \_endsOn; public RolloutPeriodBuilder StartsOn(DateTime value) { \_startsOn = value; } public RolloutPeriodBuilder EndsOn(DateTime value) { \_endsOn = value; } public Events.RolloutPeriod Build() { return new Events.RolloutPeriod(\_startsOn, \_endsOn); } } public abstract class AggregateRootEntity { Guid \_id; long \_version; protected void ApplyEvent(IEventBuilder&lt;IEvent&gt; builder) { ApplyEvent(builder.Build(\_id, ++\_version)); } private void ApplyEvent(IEvent @event) { /\* You know what goes here ... \*/ } } 
```

 Why the explicit event builder interface? Basically this is a contract between the derived aggregate root entities and an AggregateRootEntity base class, where the latter provides event sourcing capabilities as found in Greg Young's [m-r](https://github.com/gregoryyoung/m-r "CQRS/Event Sourcing sample project over on GitHub.com"). It allows the derived classes to concern themselves with the purely domain specific part of an event, while the base class can provide event identity (i.e. the aggregate identifier) and versioning (i.e. incrementing a version number upon each change or calculation a version hash or ...). The base class makes no assumption about how the information is stored inside the event, by virtue of that Build method. Having event builders is great, but where can event builders be used in your codebase? The short answer is: pretty much everywhere you'd be using an event. Be it in a command handler test to assert the proper event was produced, inside an event handler/denormalizer test to properly compose an event to be handled, or inside an aggregate to build up events in a readable manner before turning them over to the ApplyEvent(...) method. Below is an example of its usage inside an aggregate. 

```csharp
 public class Template : AggregateRootEntity { public void BeginRollout(Domain.RolloutPeriod periodToRollout) { Guard.Against( IsPeriodRolledOut(periodToRollout), ErrorCode.PeriodAlreadyRolledOut); ApplyEvent( Build.BeganTemplateRollout. ForPeriod(periodToRollout) //Double dispatch ); } public bool IsPeriodRolledOut(Domain.RolloutPeriod periodToRollout) { /\* Do what is necessary \*/ return false; } void ApplyEvent(BeganTemplateRolloutEvent @event) { // change internal state here // if you care for it } } public class RolloutPeriod { //i.e. Domain.RolloutPeriod private DateTime \_startDate; private DateTime \_endDate; internal void BuildEvent(BeganTemplateRolloutEventBuilder builder) { builder.ForPeriod( Build.RolloutPeriod. StartsOn(\_startDate). EndsOn(\_endDate)); } } internal static class Build { public static BeganTemplateRolloutEventBuilder BeganTemplateRollout { get { return new BeganTemplateRolloutEventBuilder(); } } public static RolloutPeriodBuilder RolloutPeriod { get { return new RolloutPeriodBuilder(); } } } public static class EventBuilderExtensions { public static BeganTemplateRolloutEventBuilder ForPeriod( this BeganTemplateRolloutEventBuilder builder, Domain.RolloutPeriod value) { value.BuildEvent(builder); //Double dispatch magic return builder; } } 
```


