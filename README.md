# Divverence.MarbleTesting.Akka

Divverence.MarbleTesting.Akka is a small library that allows to write tests for [Akka.Net](https://github.com/akkadotnet/akka.net/) actors / systems using marble diagrams in text form.
This library is inspired by the practice in the Rx / ReactiveStreams world to use [marble diagrams](http://rxmarbles.com/) to describe the (intended) behavior of operators.
It's our belief that this concept also applies very nicely to [Actors](https://petabridge.com/blog/akkadotnet-what-is-an-actor/) in an [actor model](https://en.wikipedia.org/wiki/Actor_model).
Inspiration for this library came from the ideas of [Erik Meijer](https://twitter.com/headinthebox) and the [marble test features](https://github.com/ReactiveX/rxjs/blob/master/doc/writing-marble-tests.md) of RxJS v5.

The purpose of the library is to help you write unit tests for your [Akka.Net](https://github.com/akkadotnet/akka.net/) actors that are as readable and concise as possible.

For background on Marble Testing in general, check this nice [7 minutes introduction to RxJs testing](https://egghead.io/lessons/rxjs-introduction-to-rxjs-marble-testing) on egghead.io.

This library is complementary to [MarbleTest.Net](https://github.com/alexvictoor/MarbleTest.Net), which targets System.Reactive (Rx.Net) specifically. Thanks go out to the author(s) of that project for ideas and possibly some source code.

## Getting Started

You can use `Divverence.MarbleTesting` in .NET and .Net Core 2 - the nuget package is available for `net452` and `netstardard`.

To get the lib just use nuget, from the nuget package console in Visual Studio:

```posh
PM> Install-Package Divverence.MarbleTesting
PM> Install-Package Divverence.MarbleTesting.Akka
```

Or add it with the dotnet CLI from `cmd`, `powershell` or `bash`:

```sh
dotnet add package Divverence.MarbleTesting.Akka
```

## Usage

Use pieces of code like this in your unit tests for you actors:

```csharp
var marbleTest = new AkkaMarbleTest( ActorSystem );
marbleTest.WhenTelling("       --a---a---b--a-c--", disctinctActor, s => s);
marbleTest.ExpectMsgs<string>("--a-------b----c--", (s, o) => o.Should().Be(s));
await marbleTest.Run();
```

```csharp
var marbleTest = new AkkaMarbleTest( Sys );
var throttleActor = Sys.ActorOf( ThrottleActor.Props(TimeSpan.FromSeconds(5)) );
marbleTest.WhenTelling("       --a---a-b-----c---------(d,e)---", throttleActor, s => s);
marbleTest.ExpectMsgs<string>("--a----a----b----c------d----e--", (s, o) => o.Should().Be(s));
var virtualTimeStep = TimeSpan.FromSeconds(1);
await marbleTest.Run(virtualTimeStep); // This runs in milliseconds!
```

## Marble Sequence syntax

The syntax for describing Sequences is compatible with the RxJS
syntax. Each character represents what happens during a moment in
virtual time.

**'`-`'** means that no message is sent/received at that moment.
**'`a`'** or any other non-reserved character means that a message is
sent or received at that moment.

So in the `WhenTelling` usage, "`X-Y-`" means:

- at time '`0`', a message `X` is sent,
- at time '`1`', nothing is sent,
- at time '`2`', a message `Y` is sent,
- at time '`3`', nothing is sent,
- before and after time '`0`' and '`3`', nothing is sent.

And in the `ExpectMsg` usage, "`X-Y-`" means:

- at time '`0`', a message `X` should arrive and nothing else,
- at time '`1`', no message should arrive,
- at time '`2`', a message `Y` should arrive and nothing else,
- at time '`3`', no message should arrive,
- before time '`0`', nothing should arrive,
- after time '`3`', no assertions are made about what should happen.

## Available (extension) methods on `AkkaMarbleTest`

### `ExpectMsg`

As explained above `ExpectMsg` allows you to specify that a particular
message should arrive at a particular `TestProbe` at a particular
moment. The 'assertion' passed has the 'marble' a `string` as its
first parameter and the `object/T` received by the probe. Note that
`ExpectMsg` is strict in the sense that it will throw an `Exception`
if at the given moment any other message is received by the
`TestProbe`.

### `WhenTelling`

`WhenTelling` is the method to use to provide _input_ to the "Subject
Under Test". The `Func<string, T>` specifies what to send for which marble.

### `WhenDoing`

`WhenDoing` is the method to use to run a particular _action_ at a
given moment. This could be used for instance, to change something in the
_environment_ to which your actor under test should respond.

### `Assert`

`Assert` can be used to run an assertion at a particular moment in
time. This could be used to assert that a _side effect_ was
accomplished by the actor under test.

### Simultaneous messages

#### Ordered groups

If messages should be sent or expected at the same time, you can group them using parenthesis.
So `--(a b c)--` means events a, b and c occur at moment 2, but will (must) be in the given order.

#### Unordered groups

To expect messages that can be in any order, use angle brackets `<`, `>`.
So `--<a b c>--` means events a, b and c occur at moment 2, and the
test framework should not care about the order of the events.


For a complete description of the syntax, please refer to the [official RxJS documentation](https://github.com/ReactiveX/rxjs/blob/master/doc/writing-marble-tests.md)

## Supported Test Frameworks

Divverence.MarbleTesting.Akka internally uses the `TestKitBase.ExpectMsg` methods, and should therefore work with any of the test frameworks supported by Akka.Net.

## Building

Use dotnet CLI 2.0:

```sh
dotnet build -c Release
```
