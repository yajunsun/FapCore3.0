﻿using System;
using System.Text;
using SQLGeneration.Parsing;

namespace SQLGeneration.Builders
{
    /// <summary>
    /// Represents an left-outer join in a select statement.
    /// </summary>
    public class LeftOuterJoin : FilteredJoin
    {
        /// <summary>
        /// Initializes a new instance of a LeftOuterJoin.
        /// </summary>
        /// <param name="leftHand">The left hand item in the join.</param>
        /// <param name="rightHand">The right hand table in the join.</param>
        internal LeftOuterJoin(Join leftHand, AliasedSource rightHand)
            : base(leftHand, rightHand)
        {
        }

        /// <summary>
        /// Provides information to the given visitor about the current builder.
        /// </summary>
        /// <param name="visitor">The visitor requesting information.</param>
        protected override void OnAccept(BuilderVisitor visitor)
        {
            visitor.VisitLeftOuterJoin(this);
        }
    }
}
