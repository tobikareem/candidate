using Recon.App.Store;
using Recon.Domain;

namespace Recon.Tests;

public class ReconStoreTests
{
    private static Activity Activity(string id) => new(
        Id: id,
        StudyId: "",
        SubjectId: "S-12-001",
        VisitLabelRaw: "Screening Visit",
        VisitLabelNorm: "Screening Visit",
        ServiceDate: new DateOnly(2026, 1, 1),
        Status: ActivityStatus.Completed,
        SourceVendor: Vendor.RealTime,
        CtaLineId: null);

    [Fact]
    public void Distinct_activities_are_all_stored_and_resolvable_by_id()
    {
        var store = new ReconStore();

        store.AddActivities(new[] { Activity("activity-aaa"), Activity("activity-bbb") });

        Assert.Equal(2, store.Activities.Count);
        Assert.NotNull(store.ActivityById("activity-aaa"));
        Assert.NotNull(store.ActivityById("activity-bbb"));
    }

    [Fact]
    public void Duplicate_activity_id_throws()
    {
        var store = new ReconStore();

        Assert.Throws<ArgumentException>(() =>
            store.AddActivities(new[] { Activity("activity-dup"), Activity("activity-dup") }));
    }

    [Fact]
    public void Throwing_on_a_duplicate_does_not_desync_list_and_lookup()
    {
        var store = new ReconStore();
        store.AddActivities(new[] { Activity("activity-aaa") });

        Assert.Throws<ArgumentException>(() =>
            store.AddActivities(new[] { Activity("activity-aaa") }));

        Assert.Single(store.Activities);
        Assert.NotNull(store.ActivityById("activity-aaa"));
    }
}
