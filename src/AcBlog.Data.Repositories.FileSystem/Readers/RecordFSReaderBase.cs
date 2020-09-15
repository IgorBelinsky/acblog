﻿using AcBlog.Data.Models;
using AcBlog.Data.Models.Actions;
using StardustDL.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace AcBlog.Data.Repositories.FileSystem.Readers
{
    public abstract class RecordFSReaderBase<T, TId, TQuery> : RecordFSRepository<T, TId, TQuery> where TId : class where T : class, IHasId<TId> where TQuery : QueryRequest, new()
    {
        protected RecordFSReaderBase(string rootPath, IFileProvider fileProvider) : base(rootPath, fileProvider)
        {

        }

        readonly Lazy<RepositoryStatus> _status = new Lazy<RepositoryStatus>(new RepositoryStatus
        {
            CanRead = true,
            CanWrite = false,
        });

        protected virtual async Task<RepositoryStatistic> GetStatistic(CancellationToken cancellationToken = default)
        {
            await using var fs = await (await FileProvider.GetFileInfo(Paths.GetStatisticFile(RootPath)).ConfigureAwait(false))
                    .CreateReadStream().ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<RepositoryStatistic>(fs, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (result is null)
                throw new Exception("Statistic is null.");
            return result;
        }

        protected virtual string GetPath(TId id) => Paths.GetDataFile(RootPath, id?.ToString() ?? throw new Exception("Id is empty."));

        public override async IAsyncEnumerable<TId> All([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using var fs = await (await FileProvider.GetFileInfo(Paths.GetIdListFile(RootPath)).ConfigureAwait(false))
                .CreateReadStream().ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<IList<TId>>(fs, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (result is not null)
            {
                foreach (var item in result)
                    yield return item;
            }
        }

        protected virtual IAsyncEnumerable<TId>? EfficientQuery(TQuery query, CancellationToken cancellationToken = default) => null;

        protected virtual IAsyncEnumerable<TId>? FullQuery(TQuery query, CancellationToken cancellationToken = default) => null;

        public override IAsyncEnumerable<TId> Query(TQuery query, CancellationToken cancellationToken = default)
        {
            if (EfficientQuery(query, cancellationToken) is IAsyncEnumerable<TId> result)
            {
                return result;
            }
            else if (FullQuery(query, cancellationToken) is IAsyncEnumerable<TId> result2)
            {
                return result2;
            }
            return AsyncEnumerable.Empty<TId>();
        }

        public override async Task<QueryStatistic> Statistic(TQuery query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query.Term))
            {
                var stats = await GetStatistic(cancellationToken);
                return new QueryStatistic
                {
                    Count = stats.Count
                };
            }
            var result = new QueryStatistic();
            result.Count = await Query(query, cancellationToken).CountAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

        public override Task<TId?> Create(T value, CancellationToken cancellationToken = default) => Task.FromResult<TId?>(null);

        public override Task<bool> Delete(TId id, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public override async Task<bool> Exists(TId id, CancellationToken cancellationToken = default)
        {
            return await (await FileProvider.GetFileInfo(GetPath(id)).ConfigureAwait(false)).Exists();
        }

        public override async Task<T?> Get(TId id, CancellationToken cancellationToken = default)
        {
            await using var fs = await (await FileProvider.GetFileInfo(GetPath(id)).ConfigureAwait(false)).CreateReadStream().ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<T?>(fs, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (result is not null)
                result.Id = id;
            return result;
        }

        public override Task<bool> Update(T value, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public override Task<RepositoryStatus> GetStatus(CancellationToken cancellationToken = default) => Task.FromResult(_status.Value);
    }
}
