// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test.Helpers
{
    public class TestRenderer : Renderer
    {
        public TestRenderer() : this(new TestServiceProvider())
        {
        }

        public TestRenderer(IDispatcher dispatcher) : base(new TestServiceProvider(), dispatcher)
        {
        }

        public TestRenderer(IServiceProvider serviceProvider) : base(serviceProvider, new RendererSynchronizationContext())
        {
        }

        public Action<RenderBatch> OnUpdateDisplay { get; set; }

        public List<CapturedBatch> Batches { get; }
            = new List<CapturedBatch>();

        public new int AssignRootComponentId(IComponent component)
            => base.AssignRootComponentId(component);

        public new void RenderRootComponent(int componentId)
            => Invoke(() => base.RenderRootComponent(componentId));

        public new Task RenderRootComponentAsync(int componentId)
            => InvokeAsync(() => base.RenderRootComponentAsync(componentId));

        public new Task RenderRootComponentAsync(int componentId, ParameterCollection parameters)
            => InvokeAsync(() => base.RenderRootComponentAsync(componentId, parameters));

        public new void DispatchEvent(int componentId, int eventHandlerId, UIEventArgs args)
        {
            var t = Invoke(() => base.DispatchEvent(componentId, eventHandlerId, args));
            // This should always be run synchronously
            Assert.True(t.IsCompleted);
            if (t.IsFaulted)
            {
                var exception = t.Exception.Flatten().InnerException;
                while (exception is AggregateException e)
                {
                    exception = e.InnerException;
                }
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        public T InstantiateComponent<T>() where T : IComponent
            => (T)InstantiateComponent(typeof(T));

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            OnUpdateDisplay?.Invoke(renderBatch);

            var capturedBatch = new CapturedBatch();
            Batches.Add(capturedBatch);

            for (var i = 0; i < renderBatch.UpdatedComponents.Count; i++)
            {
                ref var renderTreeDiff = ref renderBatch.UpdatedComponents.Array[i];
                capturedBatch.AddDiff(renderTreeDiff);
            }

            // Clone other data, as underlying storage will get reused by later batches
            capturedBatch.ReferenceFrames = renderBatch.ReferenceFrames.ToArray();
            capturedBatch.DisposedComponentIDs = renderBatch.DisposedComponentIDs.ToList();

            // This renderer updates the UI synchronously, like the WebAssembly one.
            // To test async UI updates, subclass TestRenderer and override UpdateDisplayAsync.
            return Task.CompletedTask;
        }
    }
}
