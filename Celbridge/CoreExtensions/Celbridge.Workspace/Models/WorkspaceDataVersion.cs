﻿using SQLite;

namespace Celbridge.Workspace.Models;

public class WorkspaceDataVersion
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int Version { get; set; }
}