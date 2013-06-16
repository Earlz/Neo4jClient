﻿using NUnit.Framework;
using Neo4jClient.Cypher;
using System.Collections.Generic;
using System;
using System.Linq.Expressions;

namespace Neo4jClient.Test.Cypher
{
    public class CypherWhereExpressionBuilderTests
    {
        // ReSharper disable ClassNeverInstantiated.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        class Foo
        {
            public int Bar { get; set; }
            public int? NullableBar { get; set; }
            public SomeEnum Enum { get; set; }
            public bool SomeBool { get; set; }
        }
        // ReSharper restore ClassNeverInstantiated.Local
        // ReSharper restore UnusedAutoPropertyAccessor.Local

        public enum SomeEnum
        {
            Abc,
            Def
        }

        // This must be a public static field, that's not a constant
        public static int BazField = 123;

        // This must be a public static property
        public static int BazProperty
        {
            get { return 456; }
        }

        interface IFoo
        {
            int Bar { get; set; }
        }

        [Test]
        public void AccessStaticField()
        {
            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, bool>> expression = foo => foo.Bar == BazField;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(foo.Bar = {p0})", result);
            Assert.AreEqual(123, parameters["p0"]);
        }

        [Test]
        public void AccessStaticProperty()
        {
            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, bool>> expression = foo => foo.Bar == BazProperty;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(foo.Bar = {p0})", result);
            Assert.AreEqual(456, parameters["p0"]);
        }

        [Test]
        public void EvaluateFalseWhenComparingMissingNullablePropertyToValue()
        {
            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, bool>> expression = foo => foo.NullableBar == 123;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(foo.NullableBar! = {p0})", result);
            Assert.AreEqual(123, parameters["p0"]);
        }

        [Test]
        public void EvaluateTrueWhenComparingMissingNullablePropertyToNotValue()
        {
            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, bool>> expression = foo => foo.NullableBar != 123;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(foo.NullableBar? <> {p0})", result);
            Assert.AreEqual(123, parameters["p0"]);
        }

        [Test]
        public void EvaluateTrueWhenComparingMissingNullablePropertyToNull()
        {
            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, bool>> expression = foo => foo.NullableBar == null;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(foo.NullableBar? is null)", result);
        }

        [Test]
        public void EvaluateFalseWhenComparingMissingNullablePropertyToNotNull()
        {
            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, bool>> expression = foo => foo.NullableBar != null;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(foo.NullableBar? is not null)", result);
        }

        [Test]
        public void ShouldComparePropertiesAcrossEntities()
        {
            // http://stackoverflow.com/questions/15718916/neo4jclient-where-clause-not-putting-in-parameters
            // Where<TSourceNode, TSourceNode>((otherStartNodes, startNode) => otherStartNodes.Id != startNode.Id)

            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, Foo, bool>> expression =
                (p1, p2) => p1.Bar == p2.Bar;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(p1.Bar = p2.Bar)", result);
        }

        [Test]
        public void ShouldComparePropertiesAcrossEntitiesNotEqual()
        {
            // http://stackoverflow.com/questions/15718916/neo4jclient-where-clause-not-putting-in-parameters
            // Where<TSourceNode, TSourceNode>((otherStartNodes, startNode) => otherStartNodes.Id != startNode.Id)

            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, Foo, bool>> expression =
                (p1, p2) => p1.Bar != p2.Bar;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(p1.Bar <> p2.Bar)", result);
        }

        [Test]
        public void ShouldComparePropertiesAcrossInterfaces()
        {
            // http://stackoverflow.com/questions/15718916/neo4jclient-where-clause-not-putting-in-parameters
            // Where<TSourceNode, TSourceNode>((otherStartNodes, startNode) => otherStartNodes.Id != startNode.Id)

            var parameters = new Dictionary<string, object>();
            Expression<Func<IFoo, IFoo, bool>> expression =
                (p1, p2) => p1.Bar == p2.Bar;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(p1.Bar = p2.Bar)", result);
        }

        [Test]
        [Description("https://bitbucket.org/Readify/neo4jclient/issue/73/where-clause-not-building-correctly-with")]
        public void ShouldComparePropertiesAcrossInterfacesViaGenerics()
        {
            TestShouldComparePropertiesAcrossInterfacesViaGenerics<IFoo>();
        }

        static void TestShouldComparePropertiesAcrossInterfacesViaGenerics<TNode>() where TNode : IFoo
        {
            var parameters = new Dictionary<string, object>();
            Expression<Func<TNode, TNode, bool>> expression =
                (p1, p2) => p1.Bar == p2.Bar;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(p1.Bar = p2.Bar)", result);
        }

        [Test]
        [Description("https://bitbucket.org/Readify/neo4jclient/issue/82/enumvalueconverter-and-andwhere")]
        public void ShouldCompareEnumsByNames()
        {
            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, bool>> expression =
                p1 => p1.Enum == SomeEnum.Def;

            var result = CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v));

            Assert.AreEqual("(p1.Enum = {p0})", result);
            Assert.AreEqual("Def", parameters["p0"]);
        }

        [Description("https://bitbucket.org/Readify/neo4jclient/issue/99/throw-error-when-unary-expressions-are")]
        public void ThrowNotSupportedExceptionForMemberAccessExpression()
        {
            // Where<FooData>(n => n.Bar)

            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, bool>> expression =
                p1 => p1.SomeBool;

            Assert.Throws<NotSupportedException>(() =>
                CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v)));
        }

        [Test]
        [Description("https://bitbucket.org/Readify/neo4jclient/issue/99/throw-error-when-unary-expressions-are")]
        public void ThrowNotSupportedExceptionForUnaryNotExpression()
        {
            // Where<FooData>(n => !n.Bar)

            var parameters = new Dictionary<string, object>();
            Expression<Func<Foo, bool>> expression =
                p1 => !p1.SomeBool;

            Assert.Throws<NotSupportedException>(() =>
                CypherWhereExpressionBuilder.BuildText(expression, v => CreateParameter(parameters, v)));
        }

        static string CreateParameter(IDictionary<string, object> parameters, object paramValue)
        {
            var paramName = string.Format("p{0}", parameters.Count);
            parameters.Add(paramName, paramValue);
            return "{" + paramName + "}";
        }
    }
}
