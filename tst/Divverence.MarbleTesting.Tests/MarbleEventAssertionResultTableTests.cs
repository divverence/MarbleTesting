﻿using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace Divverence.MarbleTesting.Tests
{
    public class MarbleEventAssertionResultTableTests
    {
        private const string Success = "";
        private static readonly MarbleEventAssertionResultTable.Result AFailure = new MarbleEventAssertionResultTable.Result("m", null, "❌");
        private static readonly MarbleEventAssertionResultTable.Result ASuccess = new MarbleEventAssertionResultTable.Result("m", null, Success);
        private readonly MarbleEventAssertionResultTable _table;


        public MarbleEventAssertionResultTableTests()
        {
            _table = new MarbleEventAssertionResultTable();
        }

        [Fact]
        public void AllRowsAtLeastOneSuccessShouldFailIfARowDoesNotContainASuccess()
        {
            var sut = new MarbleEventAssertionResultTable();
            sut.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                AFailure
            });
            sut.AllRowsAtLeastOneSuccess().Should().BeFalse();
        }

        [Fact]
        public void AllRowsAtLeastOneSuccessShouldBeTrueIfAllRowsContainASuccess()
        {
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                ASuccess
            });
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                AFailure,
                ASuccess
            });
            _table.AllRowsAtLeastOneSuccess().Should().BeTrue();
        }

        [Fact]
        public void MonotonicSuccessShouldReturnTrueForMonotonicSuccess()
        {
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                ASuccess
            });
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                AFailure,
                ASuccess
            });
            _table.MonotonicSuccess().Should().BeTrue();
        }

        [Fact]
        public void MonotonicSuccessShouldReturnFalseForNonMonotonicSuccess()
        {
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                AFailure,
                ASuccess
            });
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                ASuccess,
                AFailure
            });
            _table.MonotonicSuccess().Should().BeFalse();
        }

        [Fact]
        public void MonotonicSuccessShouldReturnFalseForNonStrictlyMonotonicSuccess()
        {
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                AFailure,
                ASuccess
            });
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                AFailure,
                ASuccess
            });
            _table.MonotonicSuccess().Should().BeFalse();
        }

        [Fact]
        public void MonotonicSuccessShouldReturnTrueForEmptyTable()
        {
            _table.MonotonicSuccess().Should().BeTrue();
        }

        [Fact]
        public void MonotonicSuccessShouldReturnTrueForSingleSuccessTable()
        {
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                ASuccess
            });
            _table.MonotonicSuccess().Should().BeTrue();
        }

        [Fact]
        public void AllColumnsAtLeastOneSuccessShouldReturnFalseForAllFailuresTable()
        {
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                AFailure,
            });
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                AFailure,
            });
            _table.AllColumnsAtLeastOneSuccess().Should().BeFalse();
        }

        [Fact]
        public void AllColumnsAtLeastOneSuccessShouldReturnTrueForTable()
        {
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                ASuccess,
                AFailure,
            });
            _table.AddResultsRow(new List<MarbleEventAssertionResultTable.Result>
            {
                AFailure,
                ASuccess
            });
            _table.AllColumnsAtLeastOneSuccess().Should().BeTrue();
        }
    }
}