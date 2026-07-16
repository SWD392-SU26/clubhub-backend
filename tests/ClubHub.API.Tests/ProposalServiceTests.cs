using ClubHub.API.Data;
using ClubHub.API.DTOs.Proposal;
using ClubHub.API.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClubHub.API.Tests;

public class ProposalServiceTests : ServiceTestBase
{
    [Fact]
    public async Task Approved_Proposal_Creates_Club_And_President_Membership()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Proposals(db);

        var submitted = await service.SubmitAsync(new SubmitProposalRequest(
            "AI Builders",
            ClubCategory.Technology,
            "Applied AI project club.",
            "Build useful AI prototypes.",
            "Students need a project-based AI community.",
            "Weekly labs and monthly demo days.",
            "Le Gia Han",
            "SE170003",
            null,
            "giahan@student.clubhub.local",
            "0900000003",
            null,
            "https://example.com/logos/ai-builders.png",
            "https://example.com/proposals/ai-builders.pdf",
            "Founding group has 8 students."),
            DataSeeder.StudentThreeId);

        Assert.True(submitted.IsSuccess);

        var review = await service.ReviewAsync(
            submitted.Data!.Id,
            new ReviewProposalRequest(true, null),
            DataSeeder.AdminId);

        Assert.True(review.IsSuccess);

        var club = await db.Clubs.SingleAsync(c => c.Name == "AI Builders");
        var president = await db.ClubMembers.SingleAsync(m =>
            m.ClubId == club.Id &&
            m.UserId == DataSeeder.StudentThreeId);

        Assert.Equal(ProposalStatus.Approved, await db.ClubProposals.Where(p => p.Id == submitted.Data.Id).Select(p => p.Status).SingleAsync());
        Assert.Equal(ClubRole.President, president.RoleInClub);
        Assert.Equal(MembershipStatus.Approved, president.Status);
    }

    [Fact]
    public async Task Owner_Can_Edit_And_Resubmit_NeedMoreInfo_Proposal()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Proposals(db);

        var proposal = await db.ClubProposals.SingleAsync(p => p.ClubName == "Media Studio");

        var edit = await service.UpdateAsync(
            proposal.Id,
            new UpdateProposalRequest(
                "Media Studio Updated",
                null,
                null,
                null,
                null,
                "Weekly media production workshops and monthly campus news episodes.",
                null,
                null,
                null,
                null,
                null,
                "Dr. Media Advisor",
                null,
                null,
                null),
            DataSeeder.StudentTwoId);

        var resubmit = await service.ResubmitAsync(proposal.Id, DataSeeder.StudentTwoId);

        Assert.True(edit.IsSuccess);
        Assert.True(resubmit.IsSuccess);
        Assert.Equal(ProposalStatus.Pending, proposal.Status);
        Assert.NotNull(proposal.ResubmittedAt);
    }

    [Fact]
    public async Task NonOwner_Cannot_Edit_And_Approved_Proposal_Cannot_Be_Edited()
    {
        await using var db = await CreateSeededDbAsync();
        var service = Proposals(db);

        var proposal = await db.ClubProposals.SingleAsync(p => p.ClubName == "Startup Lab");

        var nonOwnerEdit = await service.UpdateAsync(
            proposal.Id,
            new UpdateProposalRequest("Not Mine", null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            DataSeeder.StudentOneId);

        proposal.Status = ProposalStatus.Approved;
        await db.SaveChangesAsync();

        var approvedEdit = await service.UpdateAsync(
            proposal.Id,
            new UpdateProposalRequest("Already Approved", null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            DataSeeder.StudentThreeId);

        Assert.False(nonOwnerEdit.IsSuccess);
        Assert.False(approvedEdit.IsSuccess);
    }
}
