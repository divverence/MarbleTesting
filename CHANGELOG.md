<!--

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Lines should be no longer than 180 characters.
Change log entries should be formulated in the imperative present tense.

-->

# Changelog

## Unreleased

### Changed

* Change the `TestProbe` parameter in `AkkaUnorderedExpectations.CreateExpectedMarbleForUnorderedGroup` to be of type `Action<Action<T>>`.
* Change the type of the `assertionFactory` parameter from `Func<string, Action<T>>` to `Action<string, T>` in `MarbleTestExtensionsForAkka.ExpectMsgs` overloads.
* The following types are now `internal`:
    * `ExpectedMarble`
    * `ExpectedMarbles`
    * `InputMarble`
    * `Moment`
* The accessibility of the lists `Expectations` and `Inputs` on `MarbleTest` is now `private`.
* The accessibility of the methods `CreateInputMarbles` and `ParseSequence` is now `private`.
* Change different constructors of `Moment` to static factory methods.
* Change the `bool` `IsOrderedGroup` property to a `MomentType` `enum` property `Type`.

### Added

* Add methods `Assert` and `LooselyExpect` to `MarbleTest`
* Add `TimeShiftedClone` method to `Moment`.

### Deleted

* Remove `[Obsolete(...)]` `ExpectMsgs` overloads that use predicates.
* Remove `MarbleParser`, the `MultiCharMarbleParser` is the only parser that can be used.
* Remove `MarbleTest` constructors that allow specifying the marble parser to use.


## [0.7.0] - 2018-11-22

### Added

* Add optional parameter for the `nothingElseAssertion` in `MarbleTestExtensionsForAkka` `ExpectMsg` and `ExpectMsg<T>`.

### Deprecated

* Mark overloads of `ExpectMsg` that have a `Predicate` as the 'assertion' parameter as `[Obsolete]`.

## [0.6.0] - 2018-09-26

### Added

* Add support for multiple expectations that share the same 'probe'

## [0.5.0] - 2018-05-24

### Added

* Add netstandard 2.0 support, in addition to .NET 4.5.2

## [0.4.0] - 2018-01-10

### Addded

* Add support for unordered groups.

## [0.3.0] - 2017-10-27

### Fixed

* Fix handling of leading spaces in the sequence when formatting error messages.

### Added

* Add helper methods for using custom assertions asynchronously.

## [0.2.2] - 2017-09-07

### Added

* Add support for providing assertions in `ExpectMsg`.

## [0.2.1] - 2017-08-03

### Changed

* Update used version of `xunit`.

## [0.2.0] - 2017-08-03

### Added

* Support for multi character marbles.

## [0.1.0] - 2016-11-28

* First release.
