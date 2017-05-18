---
original: https://seabites.wordpress.com/2012/06/18/value-objects-in-an-eventsourced-domain-model/
title: "Value objects in an event sourced domain model"
slug: "value-objects-in-an-eventsourced-domain-model"
date: 2012-06-18
author: Yves Reynhout
publish: false
---
A question that comes up from time to time is what role [value objects](http://domaindrivendesign.org/node/135 "Value Objects") can play in an [event sourced](http://martinfowler.com/eaaDev/EventSourcing.html "Event sourcing")[domain model](http://domaindrivendesign.org/node/108 "domain layer") and how they contribute to and interact with the [events](http://en.wikipedia.org/wiki/Event "Event") produced by said domain model. Value objects come in many shapes and sizes, but obvious ones like money (amount and currency) and period (range of time values) come to mind. Fields that change together or are used together in the [ubiquitous language](http://domaindrivendesign.org/node/132 "Ubiquitous language") are prime suspects of information "clustering" and can usually be represented as value objects when they clearly lack any form of identity. They are an ideal place to [DRY](http://en.wikipedia.org/wiki/Don't_repeat_yourself "Don't repeat yourself") up some of the *validation*, *security* and *contextual capturing* code that surrounds these fields. Below you'll find a stripped version of a [value object](http://c2.com/cgi/wiki?ValueObject "Value object") from my own domain. Things you can't easily spot from the code below is the clusivity of the lower and upper boundary of this range-like value type, partially because I omitted the arithmetic operations. The finer details, such as which methods are required and who to collaborate with, surface as you discuss them with domain experts. 

```csharp
 namespace Domain { public class RolloutPeriod { readonly DateTime \_from; readonly DateTime \_to; public RolloutPeriod(DateTime from, DateTime to) { if(from &gt; to) throw new ArgumentException("The from value of the rollout period must be less than or equal to the to value.", "from"); \_from = from; \_to = to; } //Arithmetic members omitted for brevity public bool Equals(RolloutPeriod other) { if (ReferenceEquals(null, other)) return false; if (ReferenceEquals(this, other)) return true; return other.\_from.Equals(\_from) && other.\_to.Equals(\_to); } public override bool Equals(object obj) { if (ReferenceEquals(null, obj)) return false; if (ReferenceEquals(this, obj)) return true; if (obj.GetType() != typeof (RolloutPeriod)) return false; return Equals((RolloutPeriod) obj); } public override int GetHashCode() { return \_from.GetHashCode() ^ \_to.GetHashCode(); } } } 
```

 Something you do notice in the above code is that there are no getters nor setters. This is deliberate. On one hand, it allows me to redefine how I represent values on the inside (i.e. the from and to value). On the other hand, I decouple any consuming code from the internals and gently steer that code into a [Tell Don't Ask](http://pragprog.com/articles/tell-dont-ask "Tell Don't Ask") style of interaction. It makes you think about the question: "Why do you want access to my internals? What is it that you want to achieve with them? Just tell me what you want to do.". Once you get that right, you bump into the next hurdle: using value objects in an event sourced domain model. 

```csharp
 namespace Domain { public class Schedule : AggregateRootEntity { public void Rollout(RolloutPeriod period) { //Guards ommitted for brevity ApplyEvent(Build.RolledoutSchedule.ForPeriod(period)); } } public abstract class AggregateRootEntity { Guid \_id; long \_version; protected void ApplyEvent(IEventBuilder&lt;IEvent&gt; builder) { var @event = builder.Build(\_id, \_version++); //The usual stuff as found below goes here //https://github.com/gregoryyoung/m-r/blob/master/SimpleCQRS/Domain.cs } } } namespace Messaging { public static class Build { public static RolledoutScheduleBuilder RolledoutSchedule { get { return new RolledoutScheduleBuilder(); } } } public class RolledoutScheduleBuilder : IEventBuilder&lt;RolledoutScheduleEvent&gt; { RolloutPeriod \_rolloutPeriod; public RolledoutScheduleEvent Build(Guid id, long version) { return new RolledoutScheduleEvent(id, version, \_rolloutPeriod); } public RolledoutScheduleBuilder ForPeriod(RolloutPeriodBuilder builder) { \_rolloutPeriod = builder.Build(); return this; } } public interface IEventBuilder&lt;out TEvent&gt; where TEvent : IEvent { TEvent Build(Guid id, long version); } public class RolledoutScheduleEvent : IEvent { public RolledoutScheduleEvent(Guid id, long version, RolloutPeriod period) { Id = id; Version = version; Period = period; } public RolloutPeriod Period { get; private set; } public long Version { get; private set; } public Guid Id { get; private set; } } public interface IEvent { } public class RolloutPeriod { public DateTime From { get; private set; } public DateTime To { get; private set; } public RolloutPeriod(DateTime from, DateTime to) { From = from; To = to; } } public class RolloutPeriodBuilder { DateTime \_from; DateTime \_to; public RolloutPeriodBuilder From(DateTime value) { \_from = value; return this; } public RolloutPeriodBuilder To(DateTime value) { \_to = value; return this; } public RolloutPeriod Build() { return new RolloutPeriod(\_from, \_to); } } } 
```

 The rollout period passed into the Schedule rollout method, is a value object part of the domain model. The rollout period of the event - a bit obfuscated by the syntax 'Build.RolledoutSchedule.ForPeriod' above - is part of the messaging bits. How do these two meet without exposing internals?

> One thing I'd like to make clear: the domain value object IS NOT the data structure used inside the event. It's a different type.

By applying double dispatch to the domain rollout period and adding a builder extension method - inside the domain model - that is domain rollout period aware, we get a nice translation going. 

```csharp
 namespace Domain { public class RolloutPeriod { //... see above for other members internal void BuildValue(RolloutPeriodBuilder builder) { builder.From(\_from).To(\_to); } } public static class BuildExtensions { public static RolledoutScheduleBuilder ForPeriod(this RolledoutScheduleBuilder builder, RolloutPeriod period) { var valueBuilder = new RolloutPeriodBuilder(); period.BuildValue(valueBuilder); builder.ForPeriod(valueBuilder); return builder; } } } 
```

 The reverse, going from an event (or data structure within that event) to a value object, is just as easy. 

```csharp
 namespace Domain { public class RolloutPeriod { //... see above for other members internal static RolloutPeriod FromEvent(Messaging.RolloutPeriod period) { return new RolloutPeriod(period.From, period.To); } } public class Schedule : AggregateRootEntity { void Apply(RolledOutScheduleEvent @event) { \_rolloutPeriod = RolloutPeriod.FromEvent(@event.RolloutPeriod); //... } } } 
```

 So, really, there's no excuse to NOT use value objects inside an event sourced domain model nor to expose the internal state of those value objects for event sourcing purposes. Happy coding ... P.S. More information on event builders can be found [here](http://seabites.wordpress.com/2011/07/27/eventbuilders/ "Event builders").
