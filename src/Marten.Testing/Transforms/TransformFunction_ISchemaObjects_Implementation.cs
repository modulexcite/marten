﻿using Marten.Schema;
using Shouldly;
using Xunit;

namespace Marten.Testing.Transforms
{
    public class TransformFunction_ISchemaObjects_Implementation : IntegratedFixture
    {
        public TransformFunction_ISchemaObjects_Implementation()
        {
            StoreOptions(_ =>
            {
                _.Transforms.LoadFile("get_fullname.js");
            });
        }

        [Fact]
        public void can_generate_when_the_transform_is_requested()
        {
            var transform = theStore.Schema.TransformFor("get_fullname");

            theStore.Schema.DbObjects.SchemaFunctionNames()
                .ShouldContain(transform.Function);
        }

        [Fact]
        public void reset_still_makes_it_check_again()
        {
            var transform = theStore.Schema.TransformFor("get_fullname");

            theStore.Advanced.Clean.CompletelyRemoveAll();

            var transform2 = theStore.Schema.TransformFor("get_fullname");

            theStore.Schema.DbObjects.SchemaFunctionNames()
                .ShouldContain(transform2.Function);
        }

        [Fact]
        public void patch_if_it_does_not_exist()
        {
            var recorder = new DDLRecorder();

            theStore.Advanced.Options.Transforms.For("get_fullname")
                .WritePatch(theStore.Schema, recorder);

            recorder.Writer.ToString().ShouldContain("CREATE OR REPLACE FUNCTION public.mt_transform_get_fullname(doc JSONB) RETURNS JSONB AS $$");
        }

        [Fact]
        public void no_patch_if_it_does_exist()
        {
            var transform = theStore.Schema.TransformFor("get_fullname");

            var recorder = new DDLRecorder();

            theStore.Advanced.Options.Transforms.For("get_fullname")
                .WritePatch(theStore.Schema, recorder);

            recorder.Writer.ToString().ShouldNotContain("CREATE OR REPLACE FUNCTION public.mt_transform_get_fullname(doc JSONB) RETURNS JSONB AS $$");
        }

        [Fact]
        public void regenerates_if_changed()
        {
            var transform = theStore.Schema.TransformFor("get_fullname");

            theStore.Schema.DbObjects.SchemaFunctionNames()
                .ShouldContain(transform.Function);

            using (var store2 = DocumentStore.For(_ =>
            {
                _.Connection(ConnectionSource.ConnectionString);

                _.Transforms.LoadJavascript("get_fullname", "module.exports = function(){return {};}");
            }))
            {
                var transform2 = store2.Schema.TransformFor("get_fullname");


                store2.Schema.DbObjects.DefinitionForFunction(transform2.Function)
                    .ShouldContain(transform2.Body);
            }
        }
    }
}