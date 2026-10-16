using Catalog.Application.Behaviors;
using MediatR;
using System.Diagnostics.Metrics;
using Xunit;

namespace Catalog.Tests.Application
{
    public class MetricsBehaviorTests
    {
        private const string MeterName = "Catalog.Application";
        private const string DurationInstrument = "catalog.application.request.duration";
        private const string CountInstrument = "catalog.application.request.count";

        [Fact]
        public async Task Handle_SuccessfulRequest_ReturnsHandlerResponseUnchanged()
        {
            // Arrange
            var sut = new MetricsBehavior<TestRequest, string>();
            var expected = "handled";

            // Act
            var result = await sut.Handle(new TestRequest(), _ => Task.FromResult(expected), CancellationToken.None);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Handle_ThrowingRequest_RethrowsOriginalException()
        {
            // Arrange
            var sut = new MetricsBehavior<TestRequest, string>();

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.Handle(new TestRequest(), _ => throw new InvalidOperationException("boom"), CancellationToken.None));

            // Assert
            Assert.Equal("boom", exception.Message);
        }

        [Fact]
        public async Task Handle_AnyRequest_PublishesDurationAndCountInstrumentsOnApplicationMeter()
        {
            // Arrange
            var published = new List<Instrument>();
            using var listener = StartListener(published.Add);
            var sut = new MetricsBehavior<InstrumentPublishingRequest, string>();

            // Act
            await sut.Handle(new InstrumentPublishingRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

            // Assert
            var names = published.Select(i => i.Name).OrderBy(n => n).ToArray();
            Assert.Equal(new[] { CountInstrument, DurationInstrument }, names);
            Assert.All(published, i => Assert.Equal(MeterName, i.Meter.Name));
        }

        [Fact]
        public async Task Handle_AnyRequest_PublishesSingleMeterWithVersion()
        {
            // Arrange
            var published = new List<Instrument>();
            using var listener = StartListener(published.Add);
            var sut = new MetricsBehavior<VersionedMeterRequest, string>();

            // Act
            await sut.Handle(new VersionedMeterRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

            // Assert
            var meters = published.Select(i => i.Meter).Distinct().ToArray();
            var meter = Assert.Single(meters);
            Assert.Equal(MeterName, meter.Name);
            Assert.Equal("1.0.0", meter.Version);
        }

        [Fact]
        public async Task Handle_AnyRequest_PublishesDurationInstrumentWithMillisecondUnit()
        {
            // Arrange
            var published = new List<Instrument>();
            using var listener = StartListener(published.Add);
            var sut = new MetricsBehavior<DurationUnitRequest, string>();

            // Act
            await sut.Handle(new DurationUnitRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

            // Assert
            var instrument = Assert.Single(published, i => i.Name == DurationInstrument);
            Assert.Equal("ms", instrument.Unit);
        }

        private static MeterListener StartListener(Action<Instrument> onPublished)
        {
            var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name != MeterName)
                {
                    return;
                }

                onPublished(instrument);
                meterListener.EnableMeasurementEvents(instrument,
                    (double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state, object? _) => { });
            };
            listener.Start();

            return listener;
        }

        private sealed class TestRequest : IRequest<string>
        {
        }

        private sealed class InstrumentPublishingRequest : IRequest<string>
        {
        }

        private sealed class VersionedMeterRequest : IRequest<string>
        {
        }

        private sealed class DurationUnitRequest : IRequest<string>
        {
        }
    }
}