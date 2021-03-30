﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Runner.Server.Models;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class TimelineController : VssControllerBase
    {
        public static Dictionary<Guid, (List<TimelineRecord>, Dictionary<Guid, List<TimelineRecordLogLine>>)> dict = new Dictionary<Guid, (List<TimelineRecord>, Dictionary<Guid, List<TimelineRecordLogLine>>)>();

        public TimelineController()
        {

        }

        [HttpGet("{timelineId}")]
        public IEnumerable<TimelineRecord> GetTimelineRecords(Guid timelineId) {
            return dict[timelineId].Item1;
        }

        private List<TimelineRecord> MergeTimelineRecords(List<TimelineRecord> timelineRecords)
        {
            if (timelineRecords == null || timelineRecords.Count <= 1)
            {
                return timelineRecords;
            }

            Dictionary<Guid, TimelineRecord> dict = new Dictionary<Guid, TimelineRecord>();
            foreach (TimelineRecord rec in timelineRecords)
            {
                if (rec == null)
                {
                    continue;
                }

                TimelineRecord timelineRecord;
                if (dict.TryGetValue(rec.Id, out timelineRecord))
                {
                    // Merge rec into timelineRecord
                    timelineRecord.CurrentOperation = rec.CurrentOperation ?? timelineRecord.CurrentOperation;
                    timelineRecord.Details = rec.Details ?? timelineRecord.Details;
                    timelineRecord.FinishTime = rec.FinishTime ?? timelineRecord.FinishTime;
                    timelineRecord.Log = rec.Log ?? timelineRecord.Log;
                    timelineRecord.Name = rec.Name ?? timelineRecord.Name;
                    timelineRecord.RefName = rec.RefName ?? timelineRecord.RefName;
                    timelineRecord.PercentComplete = rec.PercentComplete ?? timelineRecord.PercentComplete;
                    timelineRecord.RecordType = rec.RecordType ?? timelineRecord.RecordType;
                    timelineRecord.Result = rec.Result ?? timelineRecord.Result;
                    timelineRecord.ResultCode = rec.ResultCode ?? timelineRecord.ResultCode;
                    timelineRecord.StartTime = rec.StartTime ?? timelineRecord.StartTime;
                    timelineRecord.State = rec.State ?? timelineRecord.State;
                    timelineRecord.WorkerName = rec.WorkerName ?? timelineRecord.WorkerName;

                    if (rec.ErrorCount != null && rec.ErrorCount > 0)
                    {
                        timelineRecord.ErrorCount = rec.ErrorCount;
                    }

                    if (rec.WarningCount != null && rec.WarningCount > 0)
                    {
                        timelineRecord.WarningCount = rec.WarningCount;
                    }

                    if (rec.Issues.Count > 0)
                    {
                        timelineRecord.Issues.Clear();
                        timelineRecord.Issues.AddRange(rec.Issues.Select(i => i.Clone()));
                    }

                    if (rec.Variables.Count > 0)
                    {
                        foreach (var variable in rec.Variables)
                        {
                            timelineRecord.Variables[variable.Key] = variable.Value.Clone();
                        }
                    }
                }
                else
                {
                    dict.Add(rec.Id, rec);
                }
            }

            var mergedRecords = dict.Values.ToList();
            return mergedRecords;
        }

        [HttpPatch("{scopeIdentifier}/{hubName}/{planId}/{timelineId}")]
        public async Task<IActionResult> Patch(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId)
        {
            var patch = await FromBody<VssJsonCollectionWrapper<List<TimelineRecord>>>();
            var compare = new TimelineRecord();
            if(!dict.TryAdd(timelineId, (patch.Value, new Dictionary<Guid, List<TimelineRecordLogLine>>()))) {
                dict[timelineId].Item1.AddRange(patch.Value);
                dict[timelineId] = (MergeTimelineRecords(dict[timelineId].Item1), dict[timelineId].Item2);
            }
            return await Ok(patch);
        }
    }
}