using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Bit.Client.Web.BlazorUI.Tests
{
    public abstract class BunitTestContext : IDisposable
    {
        protected Bunit.TestContext Context;

        public ITestRenderer Renderer => Context?.Renderer ?? throw new InvalidOperationException("MSTest has not started executing tests yet");

        public TestServiceProvider Services => Context?.Services ?? throw new InvalidOperationException("MSTest has not started executing tests yet");

        public void Dispose()
        {
            Context?.Dispose();
            Context = null;
        }

        [TestInitialize]
        public void Setup() => Context = new Bunit.TestContext();

        [TestCleanup]
        public void TearDown() => Dispose();

        public IRenderedComponent<TComponent> RenderComponent<TComponent>(params ComponentParameter[] parameters) where TComponent : IComponent
            => Context?.RenderComponent<TComponent>(parameters) ?? throw new InvalidOperationException("MSTest has not started executing tests yet");

        public IRenderedComponent<TComponent> RenderComponent<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder) where TComponent : IComponent
            => Context?.RenderComponent(parameterBuilder) ?? throw new InvalidOperationException("MSTest has not started executing tests yet");
    }
}
