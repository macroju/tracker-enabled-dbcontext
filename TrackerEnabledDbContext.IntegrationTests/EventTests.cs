﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrackerEnabledDbContext.Common.Configuration;
using TrackerEnabledDbContext.Common.Models;
using TrackerEnabledDbContext.Common.Testing;
using TrackerEnabledDbContext.Common.Testing.Extensions;
using TrackerEnabledDbContext.Common.Testing.Models;

namespace TrackerEnabledDbContext.IntegrationTests
{
    [TestClass]
    public class EventTests : PersistanceTests<TestTrackerContext>
    {
        [TestMethod]
        public void CanRaiseAddEvent()
        {
            using (var context = GetNewContextInstance())
            {
                EntityTracker.TrackAllProperties<TrackedModelWithMultipleProperties>();

                bool eventRaised = false;

                context.AuditLogGenerated += (sender, args) =>
                {
                    if (args.Log.EventType == EventType.Added &&
                        args.Log.TypeFullName == typeof (TrackedModelWithMultipleProperties).FullName)
                    {
                        eventRaised = true;
                    }
                };

                var entity = GetObjectFactory<TrackedModelWithMultipleProperties>().Create(false);

                entity.Description = RandomText;

                context.TrackedModelsWithMultipleProperties.Add(entity);

                context.SaveChanges();

                //assert
                Assert.IsTrue(eventRaised);

                //make sure log is saved in database
                entity.AssertAuditForAddition(context, entity.Id, null,
                    x => x.Id,
                    x => x.Description);
            }
        }

        [TestMethod]
        public void CanRaiseModifyEvent()
        {
            //TODO: modify test tracker context and identity test tracker context so that on disposal they revert the changes
            using (var context = GetNewContextInstance())
            {
                EntityTracker.TrackAllProperties<TrackedModelWithMultipleProperties>();

                bool modifyEventRaised = false;

                context.AuditLogGenerated += (sender, args) =>
                {
                    if (args.Log.EventType == EventType.Modified &&
                        args.Log.TypeFullName == typeof(TrackedModelWithMultipleProperties).FullName)
                    {
                        modifyEventRaised = true;
                    }
                };

                var existingEntity = GetObjectFactory<TrackedModelWithMultipleProperties>()
                    .Create(save: true, testDbContext:context);

                string originalValue = existingEntity.Name;
                existingEntity.Name = RandomText;

                context.SaveChanges();

                //assert
                Assert.IsTrue(modifyEventRaised);

                existingEntity.AssertAuditForModification(context, existingEntity.Id, null,
                    new AuditLogDetail
                    {
                        PropertyName = nameof(existingEntity.Name),
                        OriginalValue = originalValue,
                        NewValue = existingEntity.Name
                    });
            }
        }

        [TestMethod]
        public void CanRaiseDeleteEvent()
        {
            using (var context = GetNewContextInstance())
            {
                EntityTracker.TrackAllProperties<NormalModel>();

                bool eventRaised = false;

                context.AuditLogGenerated += (sender, args) =>
                {
                    if (args.Log.EventType == EventType.Deleted &&
                        args.Log.TypeFullName == typeof(NormalModel).FullName)
                    {
                        eventRaised = true;
                    }
                };

                var existingEntity = GetObjectFactory<NormalModel>()
                    .Create(save: true, testDbContext: context);

                context.NormalModels.Remove(existingEntity);
                context.SaveChanges();

                //assert
                Assert.IsTrue(eventRaised);

                existingEntity.AssertAuditForDeletion(context, existingEntity.Id, null,
                    x => x.Description,
                    x => x.Id);
            }
        }

        [TestMethod]
        [Ignore]
        public void CanRaiseSoftDeleteEvent()
        {
            using (var context = GetNewContextInstance())
            {
                EntityTracker.TrackAllProperties<NormalModel>();

                bool eventRaised = false;

                context.AuditLogGenerated += (sender, args) =>
                {
                    if (args.Log.EventType == EventType.Deleted &&
                        args.Log.TypeFullName == typeof(NormalModel).FullName)
                    {
                        eventRaised = true;
                    }
                };

                var existingEntity = GetObjectFactory<NormalModel>()
                    .Create(save: true, testDbContext: context);

                context.NormalModels.Remove(existingEntity);
                context.SaveChanges();

                //assert
                Assert.IsTrue(eventRaised);

                existingEntity.AssertAuditForDeletion(context, existingEntity.Id, null,
                    x => x.Description,
                    x => x.Id);
            }
        }

        [TestMethod]
        [Ignore]
        public void CanRaiseUnDeleteEvent()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CanSkipTrackingUsingEvent()
        {
            using (var context = GetNewContextInstance())
            {
                EntityTracker.TrackAllProperties<TrackedModelWithMultipleProperties>();

                bool eventRaised = false;

                context.AuditLogGenerated += (sender, args) =>
                {
                    if (args.Log.EventType == EventType.Added &&
                        args.Log.TypeFullName == typeof(TrackedModelWithMultipleProperties).FullName)
                    {
                        eventRaised = true;
                        args.SkipSaving = true;
                    }
                };

                var entity = GetObjectFactory<TrackedModelWithMultipleProperties>().Create(false);

                entity.Description = RandomText;

                context.TrackedModelsWithMultipleProperties.Add(entity);

                context.SaveChanges();

                //assert
                Assert.IsTrue(eventRaised);

                //make sure log is saved in database
                entity.AssertNoLogs(context, entity.Id, EventType.Added);
            }
        }

        private TestTrackerContext GetNewContextInstance()
        {
            return new TestTrackerContext();
        }
    }
}
