using System;
using System.Linq.Expressions;
using System.Reflection;
using Marten.Linq;
using Marten.Util;

namespace Marten.Schema
{
    public class IdField : IField
    {
        private readonly MemberInfo _idMember;

        public IdField(MemberInfo idMember)
        {
            _idMember = idMember;
        }

        public MemberInfo[] Members => new[] {_idMember};
        public string MemberName => _idMember.Name;
        public string SqlLocator { get; } = "d.id";
        public string ColumnName { get; } = "id";
        public void WritePatch(DocumentMapping mapping, IDDLRunner runner)
        {
            // Nothing
        }

        public object GetValue(Expression valueExpression)
        {
            return valueExpression.Value();
        }

        public Type MemberType => _idMember.GetMemberType();
        public bool ShouldUseContainmentOperator()
        {
            return false;
        }
    }
}