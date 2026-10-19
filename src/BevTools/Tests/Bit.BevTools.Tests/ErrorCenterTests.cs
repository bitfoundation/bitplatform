
namespace Bit.BevTools.Tests;

[TestClass]
public class ErrorCenterTests
{
    [TestMethod]
    public void Repeats_collapse_only_within_the_same_component()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var firstComponent = session.Components.Register(new ChildComponent(), typeof(ChildComponent), isBevTools: false, isHidden: false)!;
            var secondComponent = session.Components.Register(new ChildComponent(), typeof(ChildComponent), isBevTools: false, isHidden: false)!;

            session.Errors.Record(new InvalidOperationException("boom"), "render", component: firstComponent);
            session.Errors.Record(new InvalidOperationException("boom"), "render", component: firstComponent);
            session.Errors.Record(new InvalidOperationException("boom"), "render", component: secondComponent);

            var errors = session.Errors.Snapshot();
            Assert.AreEqual(2, errors.Length);
            Assert.AreEqual(2, errors.Single(e => e.ComponentInstanceId == firstComponent.InstanceId).Count);
            Assert.AreEqual(1, errors.Single(e => e.ComponentInstanceId == secondComponent.InstanceId).Count);
        }
    }

    [TestMethod]
    public void One_exception_reported_by_two_capture_paths_is_one_error()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var component = session.Components.Register(new ChildComponent(), typeof(ChildComponent), isBevTools: false, isHidden: false)!;
            var thrown = new InvalidOperationException("Render-time failure");

            // The render wrapper records it with the component, then the ErrorBoundary that caught it logs it.
            var fromRender = session.Errors.Record(thrown, "render", component: component);
            var fromLog = session.Errors.Record(thrown, "error-boundary", "Unhandled exception rendering component: Render-time failure");
            // A different exception with the same message is a separate occurrence.
            session.Errors.Record(new InvalidOperationException("Render-time failure"), "error-boundary");

            Assert.AreSame(fromRender, fromLog);
            Assert.AreEqual(2, session.Errors.Count);
            Assert.AreEqual(2, session.Errors.TotalOccurrences);
            Assert.AreEqual(1, component.ErrorCount);
        }
    }

    [TestMethod]
    public void Clear_all_resets_error_and_http_aggregate_counts()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Errors.Record(new InvalidOperationException("boom"), "test");
            var request = new BevHttpRecord
            {
                Id = session.Http.NextId(),
                StartedAt = DateTimeOffset.UtcNow,
                Method = "GET",
                Url = "https://example.test/fail",
                StatusCode = 500,
            };
            session.Http.Add(request);
            session.Http.MarkFailed(request);

            Assert.AreEqual(1, session.Errors.TotalOccurrences);
            Assert.AreEqual(1, session.Http.FailedCount);
            session.ClearAll();

            Assert.AreEqual(0, session.Errors.TotalOccurrences);
            Assert.AreEqual(0, session.Http.FailedCount);
            Assert.IsEmpty(session.Errors.Snapshot());
            Assert.IsEmpty(session.Http.Snapshot());

            session.Http.MarkFailed(request);
            Assert.AreEqual(0, session.Http.FailedCount);
        }
    }

    [TestMethod]
    public void Errors_outside_an_active_event_are_not_linked_to_an_old_click()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.LastUiEventId = session.Timeline.Record(new BevToolsEvent
            {
                Kind = BevToolsEventKind.UiEvent,
                Title = "old click",
            });

            var error = session.Errors.Record(new InvalidOperationException("later failure"), "test");

            Assert.IsNull(error.PrecedingEventId);
        }
    }

    [TestMethod]
    public void Aggregate_counts_follow_ring_buffer_eviction()
    {
        var (session, scope) = TestHelpers.CreateSession(options =>
        {
            options.MaxErrors = 2;
            options.MaxHttpRequests = 2;
        });
        using (scope)
        {
            session.Errors.Record(new InvalidOperationException("repeated"), "test");
            session.Errors.Record(new InvalidOperationException("repeated"), "test");
            session.Errors.Record(new InvalidOperationException("second"), "test");
            session.Errors.Record(new InvalidOperationException("third"), "test");

            Assert.AreEqual(2, session.Errors.Count);
            Assert.AreEqual(2, session.Errors.TotalOccurrences);

            var failed = Request(session, 500);
            session.Http.Add(failed);
            session.Http.MarkFailed(failed);
            session.Http.Add(Request(session, 200));
            session.Http.Add(Request(session, 200));

            Assert.AreEqual(2, session.Http.Count);
            Assert.AreEqual(0, session.Http.FailedCount);

            session.Http.Clear();
            var pending = Request(session, 200);
            pending.StatusCode = null;
            session.Http.Add(pending);
            pending.StatusCode = 500;
            session.Http.Add(Request(session, 200));
            session.Http.Add(Request(session, 200));
            session.Http.MarkFailed(pending);

            Assert.AreEqual(0, session.Http.FailedCount);
        }
    }

    private static BevHttpRecord Request(BevToolsSession session, int statusCode) => new()
    {
        Id = session.Http.NextId(),
        StartedAt = DateTimeOffset.UtcNow,
        Method = "GET",
        Url = "https://example.test/" + statusCode,
        StatusCode = statusCode,
    };
}
