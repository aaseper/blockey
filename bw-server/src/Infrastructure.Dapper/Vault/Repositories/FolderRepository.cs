﻿using System.Data;
using Bit.Core.Settings;
using Bit.Core.Vault.Entities;
using Bit.Core.Vault.Repositories;
using Bit.Infrastructure.Dapper.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Bit.Infrastructure.Dapper.Vault.Repositories;

public class FolderRepository : Repository<Folder, Guid>, IFolderRepository
{
    public FolderRepository(GlobalSettings globalSettings)
        : this(globalSettings.SqlServer.ConnectionString, globalSettings.SqlServer.ReadOnlyConnectionString)
    { }

    public FolderRepository(string connectionString, string readOnlyConnectionString)
        : base(connectionString, readOnlyConnectionString)
    { }

    public async Task<Folder> GetByIdAsync(Guid id, Guid userId)
    {
        var folder = await GetByIdAsync(id);
        if (folder == null || folder.UserId != userId)
        {
            return null;
        }

        return folder;
    }

    public async Task<ICollection<Folder>> GetManyByUserIdAsync(Guid userId)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<Folder>(
                $"[{Schema}].[Folder_ReadByUserId]",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);

            return results.ToList();
        }
    }
}
