﻿namespace Celbridge.Models
{
    public enum PropertyEditMode
    {
        EditEnabled,
        EditDisabled,
        Hide
    }

    public enum PropertyContext
    {
        Record,
        CelInput,
        CelOutput,
        CelInstructions
    }

    public interface IEditable
    {
        PropertyEditMode GetPropertyEditMode(PropertyContext context, string propertyName) => PropertyEditMode.EditEnabled;
    }
}