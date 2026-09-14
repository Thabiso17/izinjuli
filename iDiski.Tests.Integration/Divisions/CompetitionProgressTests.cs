using FluentAssertions;
using iDiski.Domain.Entities;
using iDiski.Domain.Services;
using Xunit;

namespace iDiski.Tests.Integration.Divisions;

/// <summary>
/// When a competition counts as finished.
///
/// Pure rules over three numbers, so no database and no fixture: this is the one piece of the
/// feature that decides what every screen says, and it should be checkable on its own.
///
/// The rule differs by format on purpose, and the two cases worth holding onto are the ones a
/// single "nothing left to play" rule gets wrong in opposite directions — a cup with a group
/// fixture nobody ever played, and a league with a fixture that was called off.
/// </summary>
public class CompetitionProgressTests
{
    [Theory]
    [InlineData(CompetitionFormat.League)]
    [InlineData(CompetitionFormat.Knockout)]
    [InlineData(CompetitionFormat.GroupAndKnockout)]
    public void NothingPlayedIsNotStarted_WhateverTheFormat(CompetitionFormat format)
    {
        // A cup draws its whole bracket on day one. That must not read as under way before
        // anybody has kicked a ball — drawing it in advance is the point of a bracket.
        CompetitionProgress.Status(format, played: 0, pending: 12, finalPlayed: false)
            .Should().Be(CompetitionStatus.NotStarted);
    }

    [Fact]
    public void ADivisionWithNoFixturesAtAll_IsNotStarted()
    {
        CompetitionProgress.Status(CompetitionFormat.League, 0, 0, false)
            .Should().Be(CompetitionStatus.NotStarted);
    }

    // ── A league is over when there is nothing left to play ───────────────────

    [Fact]
    public void ALeagueWithFixturesLeft_IsInProgress()
    {
        CompetitionProgress.Status(CompetitionFormat.League, played: 10, pending: 2, finalPlayed: false)
            .Should().Be(CompetitionStatus.InProgress);
    }

    [Fact]
    public void ALeagueWithNothingLeft_IsCompleted()
    {
        CompetitionProgress.Status(CompetitionFormat.League, played: 12, pending: 0, finalPlayed: false)
            .Should().Be(CompetitionStatus.Completed);
    }

    [Fact]
    public void ALeagueIsNotHeldOpenByAFixtureThatWasCalledOff()
    {
        // A cancelled fixture is counted in neither number: it will never be played, and a
        // season that can never close is worse than one that closes a match early.
        CompetitionProgress.Status(CompetitionFormat.League, played: 11, pending: 0, finalPlayed: false)
            .Should().Be(CompetitionStatus.Completed);
    }

    // ── A cup is over when the final is played, and only then ─────────────────

    [Theory]
    [InlineData(CompetitionFormat.Knockout)]
    [InlineData(CompetitionFormat.GroupAndKnockout)]
    public void ACupWithItsFinalUnplayed_IsInProgress(CompetitionFormat format)
    {
        CompetitionProgress.Status(format, played: 14, pending: 1, finalPlayed: false)
            .Should().Be(CompetitionStatus.InProgress);
    }

    [Theory]
    [InlineData(CompetitionFormat.Knockout)]
    [InlineData(CompetitionFormat.GroupAndKnockout)]
    public void ACupWhoseFinalIsPlayed_IsCompleted(CompetitionFormat format)
    {
        CompetitionProgress.Status(format, played: 15, pending: 0, finalPlayed: true)
            .Should().Be(CompetitionStatus.Completed);
    }

    [Fact]
    public void ACupIsFinishedOnceItsFinalIsPlayed_EvenWithAGroupGameNobodyEverPlayed()
    {
        // The case that argues for the whole format-specific rule. A group fixture abandoned
        // and never rearranged is common, and counting fixtures would leave a tournament that
        // was won in front of a crowd reading "in progress" for ever.
        CompetitionProgress.Status(
            CompetitionFormat.GroupAndKnockout, played: 14, pending: 1, finalPlayed: true)
            .Should().Be(CompetitionStatus.Completed);
    }

    // ── What "current" means ──────────────────────────────────────────────────

    [Theory]
    [InlineData(CompetitionStatus.NotStarted, true)]
    [InlineData(CompetitionStatus.InProgress, true)]
    [InlineData(CompetitionStatus.Completed, false)]
    public void CurrentMeansOnNowOrStillToCome(CompetitionStatus status, bool expected)
    {
        // A competition that has not started yet is still one a reader would call current —
        // filtering it out with the finished ones would hide next season from everybody.
        CompetitionProgress.IsCurrent(status).Should().Be(expected);
    }
}
