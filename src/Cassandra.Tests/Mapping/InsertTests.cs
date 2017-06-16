﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Cassandra.Mapping;
using Cassandra.Tasks;
using Cassandra.Tests.Mapping.FluentMappings;
using Cassandra.Tests.Mapping.Pocos;
using Cassandra.Tests.Mapping.TestData;
using Moq;
using NUnit.Framework;

namespace Cassandra.Tests.Mapping
{
    public class InsertTests : MappingTestBase
    {
        [Test]
        public void InsertAsync_Poco()
        {
            // Get a "new" user by using the test data from an existing user and changing the primary key
            var user = TestDataHelper.GetUserList().First();
            var newUser = new InsertUser
            {
                Id = Guid.NewGuid(),
                Name = user.Name,
                Age = user.Age,
                CreatedDate = user.CreatedDate,
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate,
                LoginHistory = user.LoginHistory,
                LuckyNumbers = user.LuckyNumbers,
                ChildrenAges = new Dictionary<string, int>(user.ChildrenAges),
                FavoriteColor = user.FavoriteColor,
                TypeOfUser = user.TypeOfUser,
                PreferredContact = user.PreferredContactMethod,
                HairColor = user.HairColor
            };

            var sessionMock = new Mock<ISession>(MockBehavior.Strict);
            sessionMock
                .Setup(s => s.ExecuteAsync(It.IsAny<BoundStatement>()))
                .Returns(TaskHelper.ToTask(new RowSet()))
                .Verifiable();
            sessionMock
                .Setup(s => s.PrepareAsync(It.IsAny<string>()))
                .Returns<string>(cql => TaskHelper.ToTask(GetPrepared(cql)))
                .Verifiable();
            var mappingClient = GetMappingClient(sessionMock);
            //Execute Insert and wait
            mappingClient.InsertAsync(newUser).Wait(3000);
            sessionMock.Verify(s => s.ExecuteAsync(It.Is<BoundStatement>(stmt => 
                stmt.QueryValues.Length == TestHelper.ToDictionary(newUser).Count &&
                stmt.PreparedStatement.Cql.StartsWith("INSERT INTO users (")
                )), Times.Exactly(1));
            sessionMock.Verify();
        }

        [Test]
        public void Insert_Poco()
        {
            //Just a few props as it is just to test that it runs
            var user = TestDataHelper.GetUserList().First();
            var newUser = new InsertUser
            {
                Id = Guid.NewGuid(),
                Name = user.Name
            };

            var sessionMock = new Mock<ISession>(MockBehavior.Strict);
            sessionMock
                .Setup(s => s.ExecuteAsync(It.IsAny<BoundStatement>()))
                .Returns(TestHelper.DelayedTask(new RowSet()))
                .Verifiable();
            sessionMock
                .Setup(s => s.PrepareAsync(It.IsAny<string>()))
                .Returns<string>(cql => TaskHelper.ToTask(GetPrepared(cql)))
                .Verifiable();
            var mappingClient = GetMappingClient(sessionMock);
            //Execute
            mappingClient.Insert(newUser);
            sessionMock.Verify(s => s.ExecuteAsync(It.Is<BoundStatement>(stmt =>
                stmt.QueryValues.Length == TestHelper.ToDictionary(newUser).Count &&
                stmt.PreparedStatement.Cql.StartsWith("INSERT INTO users (")
                )), Times.Exactly(1));
            sessionMock.Verify();
        }

        [Test]
        public void InsertAsync_FluentPoco()
        {
            // Get a "new" user by using the test data from an existing user and changing the primary key
            var user = TestDataHelper.GetUserList().First();
            var newUser = new FluentUser
            {
                Id = Guid.NewGuid(),
                Name = user.Name,
                Age = user.Age,
                CreatedDate = user.CreatedDate,
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate,
                LoginHistory = user.LoginHistory,
                LuckyNumbers = user.LuckyNumbers,
                ChildrenAges = new Dictionary<string,int>(user.ChildrenAges),
                FavoriteColor = user.FavoriteColor,
                TypeOfUser = user.TypeOfUser,
                PreferredContact = user.PreferredContactMethod,
                HairColor = user.HairColor
            };

            var sessionMock = new Mock<ISession>(MockBehavior.Strict);
            sessionMock
                .Setup(s => s.ExecuteAsync(It.IsAny<BoundStatement>()))
                .Returns(TaskHelper.ToTask(new RowSet()))
                .Verifiable();
            sessionMock
                .Setup(s => s.PrepareAsync(It.IsAny<string>()))
                .Returns<string>(cql => TestHelper.DelayedTask(GetPrepared(cql)))
                .Verifiable();

            // Insert the new user
            var mappingClient = GetMappingClient(sessionMock);
            mappingClient.InsertAsync(newUser).Wait(3000);

            sessionMock.Verify(s => s.ExecuteAsync(It.Is<BoundStatement>(stmt =>
                stmt.QueryValues.Length > 0 &&
                stmt.PreparedStatement.Cql.StartsWith("INSERT INTO")
                )), Times.Exactly(1));
            sessionMock.Verify();
        }

        [Test]
        public void Insert_Udt()
        {
            var album = new Album
            {
                Id = Guid.NewGuid(),
                Name = "Images and Words",
                PublishingDate = DateTimeOffset.Now,
                Songs = new List<Song>
                {
                    new Song {Artist = "Dream Theater", Title = "Pull me under"},
                    new Song {Artist = "Dream Theater", Title = "Under a glass moon"}
                }
            };
            var sessionMock = new Mock<ISession>(MockBehavior.Strict);
            sessionMock
                .Setup(s => s.ExecuteAsync(It.IsAny<BoundStatement>()))
                .Returns(TaskHelper.ToTask(new RowSet()))
                .Verifiable();
            sessionMock
                .Setup(s => s.PrepareAsync(It.IsAny<string>()))
                .Returns<string>(cql => TaskHelper.ToTask(GetPrepared(cql)))
                .Verifiable();
            var mapper = GetMappingClient(sessionMock);
            mapper.Insert(album);
            sessionMock.Verify(s => s.ExecuteAsync(It.Is<BoundStatement>(stmt =>
                stmt.QueryValues.Length > 0 &&
                stmt.PreparedStatement.Cql == "INSERT INTO Album (Id, Name, PublishingDate, Songs) VALUES (?, ?, ?, ?)"
                )), Times.Exactly(1));
            sessionMock.Verify();
        }

        [Test]
        public void Insert_Without_Nulls()
        {
            var album = new Album
            {
                Id = Guid.NewGuid(),
                Name = null,
                PublishingDate = DateTimeOffset.Now,
                Songs = null
            };
            var sessionMock = new Mock<ISession>(MockBehavior.Strict);
            string query = null;
            object[] parameters = null;
            sessionMock
                .Setup(s => s.ExecuteAsync(It.IsAny<BoundStatement>()))
                .Returns(TaskHelper.ToTask(new RowSet()))
                .Callback<BoundStatement>(stmt =>
                {
                    query = stmt.PreparedStatement.Cql;
                    parameters = stmt.QueryValues;
                })
                .Verifiable();
            sessionMock
                .Setup(s => s.PrepareAsync(It.IsAny<string>()))
                .Returns<string>(cql => TaskHelper.ToTask(GetPrepared(cql)))
                .Verifiable();
            var mapper = GetMappingClient(sessionMock);
            //with nulls by default
            mapper.Insert(album);
            Assert.AreEqual("INSERT INTO Album (Id, Name, PublishingDate, Songs) VALUES (?, ?, ?, ?)", query);
            CollectionAssert.AreEqual(new object[] { album.Id, null, album.PublishingDate, null}, parameters);
            //Without nulls
            mapper.Insert(album, false);
            Assert.AreEqual("INSERT INTO Album (Id, PublishingDate) VALUES (?, ?)", query);
            CollectionAssert.AreEqual(new object[] { album.Id, album.PublishingDate }, parameters);
            sessionMock.Verify();
        }

        [Test]
        public void Insert_Poco_Returns_WhenResponse_IsReceived()
        {
            var newUser = new InsertUser
            {
                Id = Guid.NewGuid(),
                Name = "Dummy"
            };

            var rowsetReturned = false;
            var sessionMock = new Mock<ISession>(MockBehavior.Strict);
            sessionMock
                .Setup(s => s.ExecuteAsync(It.IsAny<BoundStatement>()))
                .Returns(TestHelper.DelayedTask(new RowSet(), 2000).ContinueWith(t =>
                {
                    rowsetReturned = true;
                    return t.Result;
                }))
                .Verifiable();
            sessionMock
                .Setup(s => s.PrepareAsync(It.IsAny<string>()))
                .Returns<string>(cql => TaskHelper.ToTask(GetPrepared(cql)))
                .Verifiable();
            var mappingClient = GetMappingClient(sessionMock);
            //Execute
            mappingClient.Insert(newUser);
            Assert.True(rowsetReturned);
            sessionMock.Verify();
        }

        [Test]
        public void InsertIfNotExists_Poco_AppliedInfo_True_Test()
        {
            //Just a few props as it is just to test that it runs
            var user = TestDataHelper.GetUserList().First();
            var newUser = new InsertUser
            {
                Id = Guid.NewGuid(),
                Name = user.Name
            };
            string query = null;
            var sessionMock = new Mock<ISession>(MockBehavior.Strict);
            sessionMock
                .Setup(s => s.ExecuteAsync(It.IsAny<BoundStatement>()))
                .Returns(TestHelper.DelayedTask(TestDataHelper.CreateMultipleValuesRowSet(new [] {"[applied]"}, new [] { true})))
                .Callback<BoundStatement>(b => query = b.PreparedStatement.Cql)
                .Verifiable();
            sessionMock
                .Setup(s => s.PrepareAsync(It.IsAny<string>()))
                .Returns<string>(cql => TestHelper.DelayedTask(GetPrepared(cql)))
                .Verifiable();
            var mappingClient = GetMappingClient(sessionMock);
            //Execute
            var appliedInfo = mappingClient.InsertIfNotExists(newUser);
            sessionMock.Verify();
            StringAssert.StartsWith("INSERT INTO users (", query);
            StringAssert.EndsWith(") IF NOT EXISTS", query);
            Assert.True(appliedInfo.Applied);
        }

        [Test]
        public void InsertIfNotExists_Poco_AppliedInfo_False_Test()
        {
            //Just a few props as it is just to test that it runs
            var user = TestDataHelper.GetUserList().First();
            var newUser = new InsertUser
            {
                Id = Guid.NewGuid(),
                Name = user.Name
            };
            string query = null;
            var sessionMock = new Mock<ISession>(MockBehavior.Strict);
            sessionMock
                .Setup(s => s.ExecuteAsync(It.IsAny<BoundStatement>()))
                .Returns(TestHelper.DelayedTask(TestDataHelper.CreateMultipleValuesRowSet(new[] { "[applied]", "userid", "name" }, new object[] { false, newUser.Id, "existing-name"})))
                .Callback<BoundStatement>(b => query = b.PreparedStatement.Cql)
                .Verifiable();
            sessionMock
                .Setup(s => s.PrepareAsync(It.IsAny<string>()))
                .Returns<string>(cql => TestHelper.DelayedTask(GetPrepared(cql)))
                .Verifiable();
            var mappingClient = GetMappingClient(sessionMock);
            //Execute
            var appliedInfo = mappingClient.InsertIfNotExists(newUser);
            sessionMock.Verify();
            StringAssert.StartsWith("INSERT INTO users (", query);
            StringAssert.EndsWith(") IF NOT EXISTS", query);
            Assert.False(appliedInfo.Applied);
            Assert.AreEqual(newUser.Id, appliedInfo.Existing.Id);
            Assert.AreEqual("existing-name", appliedInfo.Existing.Name);
        }

        [Test]
        public void Insert_With_Ttl_Test()
        {
            string query = null;
            object[] parameters = null;
            var mapper = GetMappingClient(() => TaskHelper.ToTask(RowSet.Empty()), (q, p) =>
            {
                query = q;
                parameters = p;
            });
            var song = new Song { Id = Guid.NewGuid() };
            const int ttl = 600;
            mapper.Insert(song, true, ttl);
            Assert.AreEqual("INSERT INTO Song (Id, Title, Artist, ReleaseDate) VALUES (?, ?, ?, ?) USING TTL ?", query);
            Assert.AreEqual(song.Id, parameters[0]);
            Assert.AreEqual(ttl, parameters.Last());
        }

        [Test]
        public void InsertIfNotExists_With_Ttl_Test()
        {
            string query = null;
            object[] parameters = null;
            var mapper = GetMappingClient(() => TaskHelper.ToTask(RowSet.Empty()), (q, p) =>
            {
                query = q;
                parameters = p;
            });
            var song = new Song { Id = Guid.NewGuid(), Title = "t2", ReleaseDate = DateTimeOffset.Now };
            const int ttl = 600;
            mapper.InsertIfNotExists(song, false, ttl);
            Assert.AreEqual("INSERT INTO Song (Id, Title, ReleaseDate) VALUES (?, ?, ?) IF NOT EXISTS USING TTL ?", query);
            Assert.AreEqual(song.Id, parameters[0]);
            Assert.AreEqual(song.Title, parameters[1]);
            Assert.AreEqual(song.ReleaseDate, parameters[2]);
            Assert.AreEqual(ttl, parameters[3]);
        }

        [Test]
        public void Insert_SetTimestamp_Test()
        {
            BoundStatement statement = null;
            var sessionMock = new Mock<ISession>(MockBehavior.Strict);
            sessionMock.Setup(s => s.Cluster).Returns((ICluster)null);
            sessionMock
                .Setup(s => s.ExecuteAsync(It.IsAny<BoundStatement>()))
                .Returns(() => TestHelper.DelayedTask(RowSet.Empty()))
                .Callback<BoundStatement>(stmt => statement = stmt)
                .Verifiable();
            sessionMock
                .Setup(s => s.PrepareAsync(It.IsAny<string>()))
                .Returns<string>(query => TaskHelper.ToTask(GetPrepared(query)))
                .Verifiable();
            var mapper = GetMappingClient(sessionMock);
            var song = new Song { Id = Guid.NewGuid(), Title = "t2", ReleaseDate = DateTimeOffset.Now };
            var timestamp = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1));
            mapper.Insert(song);
            Assert.Null(statement.Timestamp);
            mapper.Insert(song, CqlQueryOptions.New().SetTimestamp(timestamp));
            Assert.AreEqual(timestamp, statement.Timestamp);
            timestamp = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(10));
            mapper.InsertIfNotExists(song, CqlQueryOptions.New().SetTimestamp(timestamp));
            Assert.AreEqual(timestamp, statement.Timestamp);
        }
    }
}