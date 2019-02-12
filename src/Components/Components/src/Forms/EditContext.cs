// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Holds metadata related to a data editing process, such as flags to indicate which
    /// fields have been modified and the current set of validation messages.
    /// </summary>
    public class EditContext
    {
        private Dictionary<FieldIdentifier, FieldState> _fieldStates = new Dictionary<FieldIdentifier, FieldState>();

        /// <summary>
        /// Constructs an instance of <see cref="EditContext"/>.
        /// </summary>
        /// <param name="model">The model object for the <see cref="EditContext"/>. This object should hold the data being edited, for example as a set of properties.</param>
        public EditContext(object model)
        {
            // The only reason we disallow null is because you'd almost always want one, and if you
            // really don't, you can pass an empty object then ignore it. Ensuring it's nonnull
            // simplifies things for all consumers of EditContext.
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>
        /// An event that is raised when a field value changes.
        /// </summary>
        public event EventHandler<FieldIdentifier> OnFieldChanged;

        /// <summary>
        /// Supplies a <see cref="FieldIdentifier"/> corresponding to a specified field name
        /// on this <see cref="EditContext"/>'s <see cref="Model"/>.
        /// </summary>
        /// <param name="fieldName">The name of the editable field.</param>
        /// <returns>A <see cref="FieldIdentifier"/> corresponding to a specified field name on this <see cref="EditContext"/>'s <see cref="Model"/>.</returns>
        public FieldIdentifier Field(string fieldName)
            => new FieldIdentifier(Model, fieldName);

        /// <summary>
        /// Gets the model object for this <see cref="EditContext"/>.
        /// </summary>
        public object Model { get; }

        /// <summary>
        /// Signals that the value for the specified field has changed.
        /// </summary>
        /// <param name="fieldIdentifier">Identifies the field whose value has been changed.</param>
        public void NotifyFieldChanged(FieldIdentifier fieldIdentifier)
        {
            var state = GetOrCreateFieldState(fieldIdentifier);
            state.IsModified = true;
            OnFieldChanged?.Invoke(this, fieldIdentifier);
        }

        /// <summary>
        /// Clears any modification flag that may be tracked for the specified field.
        /// </summary>
        /// <param name="fieldIdentifier">Identifies the field whose modification flag (if any) should be cleared.</param>
        public void MarkAsUnmodified(FieldIdentifier fieldIdentifier)
        {
            if (_fieldStates.TryGetValue(fieldIdentifier, out var state))
            {
                state.IsModified = false;
            }
        }

        /// <summary>
        /// Clears all modification flags within this <see cref="EditContext"/>.
        /// </summary>
        public void MarkAsUnmodified()
        {
            foreach (var state in _fieldStates.Values)
            {
                state.IsModified = false;
            }
        }

        /// <summary>
        /// Determines whether any of the fields in this <see cref="EditContext"/> have been modified.
        /// </summary>
        /// <returns>True if any of the fields in this <see cref="EditContext"/> have been modified; otherwise false.</returns>
        public bool IsModified()
            // If necessary, we could consider caching the overall "is modified" state and only recomputing
            // when there's a call to NotifyFieldModified/NotifyFieldUnmodified
            => _fieldStates.Values.Any(state => state.IsModified);

        /// <summary>
        /// Determines whether the specified fields in this <see cref="EditContext"/> has been modified.
        /// </summary>
        /// <returns>True if the field has been modified; otherwise false.</returns>
        public bool IsModified(FieldIdentifier fieldIdentifier)
            => _fieldStates.TryGetValue(fieldIdentifier, out var state)
            ? state.IsModified
            : false;

        private FieldState GetOrCreateFieldState(FieldIdentifier fieldIdentifier)
        {
            if (!_fieldStates.TryGetValue(fieldIdentifier, out var state))
            {
                state = new FieldState();
                _fieldStates.Add(fieldIdentifier, state);
            }

            return state;
        }
    }
}