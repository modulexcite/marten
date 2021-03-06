﻿using System;
using System.Linq;
using Baseline;
using Marten.Schema;
using Marten.Testing.Documents;
using Marten.Transforms;
using Marten.Util;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Marten.Testing.Transforms
{
    public class TransformFunctionTests
    {
        private readonly ITestOutputHelper _output;

        public TransformFunctionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void derive_function_name_from_logical_name()
        {
            var func = new TransformFunction(new StoreOptions(), "something",
                "module.exports = function(doc){return doc;};");


            func.Function.Name.ShouldBe("mt_transform_something");
        }

        [Fact]
        public void derive_function_with_periods_in_the_name()
        {
            var func = new TransformFunction(new StoreOptions(), "nfl.team.chiefs",
                "module.exports = function(doc){return doc;};");

            func.Function.Name.ShouldBe("mt_transform_nfl_team_chiefs");
        }

        [Fact]
        public void picks_up_the_schema_from_storeoptions()
        {
            var options = new StoreOptions
            {
                DatabaseSchemaName = "other"
            };

            var func = new TransformFunction(options, "nfl.team.chiefs",
                "module.exports = function(doc){return doc;};");


            func.Function.Schema.ShouldBe("other");

        }

        [Fact]
        public void create_function_for_file()
        {
            var options = new StoreOptions();
            var func = TransformFunction.ForFile(options, "get_fullname.js");

            func.Name.ShouldBe("get_fullname");

            func.Body.ShouldContain("module.exports");

            func.Function.Name.ShouldBe("mt_transform_get_fullname");
        }

        [Fact]
        public void rebuilds_if_it_does_not_exist_in_the_schema_if_auto_create_is_all()
        {
            var schema = Substitute.For<IDocumentSchema>();
            var dbobjects = Substitute.For<IDbObjects>();
            schema.DbObjects.Returns(dbobjects);

            var func = TransformFunction.ForFile(new StoreOptions {AutoCreateSchemaObjects = AutoCreate.All}, "get_fullname.js");

            dbobjects.SchemaFunctionNames().Returns(Enumerable.Empty<FunctionName>());

            var runner = Substitute.For<IDDLRunner>();


            func.GenerateSchemaObjectsIfNecessary(AutoCreate.All, schema, runner);

            var generated = func.GenerateFunction();

            runner.Received().Apply(func, generated);
        }

        [Fact]
        public void rebuilds_if_it_does_not_exist_in_the_schema_if_auto_create_is_create_only()
        {
            var schema = Substitute.For<IDocumentSchema>();
            var dbobjects = Substitute.For<IDbObjects>();
            schema.DbObjects.Returns(dbobjects);

            var func = TransformFunction.ForFile(new StoreOptions { AutoCreateSchemaObjects = AutoCreate.CreateOnly }, "get_fullname.js");

            dbobjects.SchemaFunctionNames().Returns(Enumerable.Empty<FunctionName>());

            var runner = Substitute.For<IDDLRunner>();


            func.GenerateSchemaObjectsIfNecessary(AutoCreate.CreateOnly, schema, runner);

            var generated = func.GenerateFunction();

            runner.Received().Apply(func, generated);
        }

        [Fact]
        public void throws_exception_if_auto_create_is_none_and_the_function_does_not_exist()
        {
            var schema = Substitute.For<IDocumentSchema>();
            var dbobjects = Substitute.For<IDbObjects>();
            schema.DbObjects.Returns(dbobjects);

            var func = TransformFunction.ForFile(new StoreOptions { AutoCreateSchemaObjects = AutoCreate.None }, "get_fullname.js");

            dbobjects.SchemaFunctionNames().Returns(Enumerable.Empty<FunctionName>());

            var runner = Substitute.For<IDDLRunner>();


            Exception<InvalidOperationException>.ShouldBeThrownBy(() =>
            {
                func.GenerateSchemaObjectsIfNecessary(AutoCreate.None, schema, runner);
            });
        }

        [Fact]
        public void rebuilds_if_it_does_not_exist_in_the_schema_if_auto_create_is_create_or_update()
        {
            var schema = Substitute.For<IDocumentSchema>();
            var dbobjects = Substitute.For<IDbObjects>();
            schema.DbObjects.Returns(dbobjects);

            var func = TransformFunction.ForFile(new StoreOptions { AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate }, "get_fullname.js");

            dbobjects.SchemaFunctionNames().Returns(Enumerable.Empty<FunctionName>());

            var runner = Substitute.For<IDDLRunner>();


            func.GenerateSchemaObjectsIfNecessary(AutoCreate.CreateOrUpdate, schema, runner);

            var generated = func.GenerateFunction();

            runner.Received().Apply(func, generated);
        }

        [Fact]
        public void does_not_regenerate_the_function_if_it_exists()
        {
            var schema = Substitute.For<IDocumentSchema>();
            var dbobjects = Substitute.For<IDbObjects>();
            schema.DbObjects.Returns(dbobjects);

            var func = TransformFunction.ForFile(new StoreOptions(), "get_fullname.js");

            dbobjects.DefinitionForFunction(func.Function).Returns(func.GenerateFunction());

            var runner = Substitute.For<IDDLRunner>();

            func.GenerateSchemaObjectsIfNecessary(AutoCreate.All, schema, runner);

            var generated = func.GenerateFunction();

            runner.DidNotReceive().Apply(func, generated);
        }

        [Fact]
        public void end_to_end_test_using_the_transform()
        {
            using (var store = TestingDocumentStore.Basic())
            {
                var user = new User {FirstName = "Jeremy", LastName = "Miller"};
                var json = new JilSerializer().ToCleanJson(user);

                var func = TransformFunction.ForFile(new StoreOptions(), "get_fullname.js");

                using (var conn = store.Advanced.OpenConnection())
                {
                    conn.Execute(cmd => cmd.Sql(func.GenerateFunction()).ExecuteNonQuery());

                    var actual = conn.Execute(cmd =>
                    {
                        return cmd.Sql("select mt_transform_get_fullname(:json)")
                            .WithJsonParameter("json", json).ExecuteScalar().As<string>();
                    });

                    actual.ShouldBe("{\"fullname\": \"Jeremy Miller\"}");
                }

                
            }
        }


    }


}