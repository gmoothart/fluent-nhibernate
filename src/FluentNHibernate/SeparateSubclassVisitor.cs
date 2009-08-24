using System;
using System.Collections.Generic;
using System.Linq;
using FluentNHibernate.Mapping.Providers;
using FluentNHibernate.MappingModel;
using FluentNHibernate.MappingModel.ClassBased;

namespace FluentNHibernate
{
    public class SeparateSubclassVisitor : DefaultMappingModelVisitor
    {
        private readonly IList<IIndeterminateSubclassMappingProvider> subclassProviders;

        public SeparateSubclassVisitor(IList<IIndeterminateSubclassMappingProvider> subclassProviders)
        {
            this.subclassProviders = subclassProviders;
        }

        public override void ProcessClass(ClassMapping mapping)
        {
            var subclasses = FindClosestSubclasses(mapping.Type);

            foreach (var provider in subclasses)
                mapping.AddSubclass(provider.GetSubclassMapping(CreateSubclass(mapping)));

            base.ProcessClass(mapping);
        }

        private IEnumerable<IIndeterminateSubclassMappingProvider> FindClosestSubclasses(Type type)
        {
            var subclasses = SortByDistanceFrom(type, subclassProviders);

            if (subclasses.Keys.Count == 0)
                return new IIndeterminateSubclassMappingProvider[0];

            var lowestDistance = subclasses.Keys.Min();

            return subclasses[lowestDistance];
        }

        public override void ProcessSubclass(SubclassMapping mapping)
        {
            var subclasses = FindClosestSubclasses(mapping.Type);

            foreach (var provider in subclasses)
                mapping.AddSubclass(provider.GetSubclassMapping(new SubclassMapping()));

            base.ProcessSubclass(mapping);
        }

        public override void ProcessJoinedSubclass(JoinedSubclassMapping mapping)
        {
            var subclasses = FindClosestSubclasses(mapping.Type);

            foreach (var provider in subclasses)
                mapping.AddSubclass(provider.GetSubclassMapping(new JoinedSubclassMapping()));

            base.ProcessJoinedSubclass(mapping);
        }

        private ISubclassMapping CreateSubclass(ClassMapping mapping)
        {
            if (mapping.Discriminator == null)
                return new JoinedSubclassMapping();
            
            return new SubclassMapping();
        }

        /// <summary>
        /// Takes a type that represents the level in the class/subclass-hiearchy that we're starting from, the parent,
        /// this can be a class or subclass; also takes a list of subclass providers. The providers are then iterated
        /// and added to a dictionary key'd by the types "distance" from the parentType; distance being the number of levels
        /// between parentType and the subclass-type.
        /// </summary>
        /// <param name="parentType">Starting point, parent type.</param>
        /// <param name="providers">List of subclasses</param>
        /// <returns>Dictionary key'd by the distance from the parentType.</returns>
        private IDictionary<int, IList<IIndeterminateSubclassMappingProvider>> SortByDistanceFrom(Type parentType, IEnumerable<IIndeterminateSubclassMappingProvider> providers)
        {
            var arranged = new Dictionary<int, IList<IIndeterminateSubclassMappingProvider>>();

            foreach (var provider in providers)
            {
                var subclassType = provider.EntityType;
                var level = 0;

                while (subclassType.BaseType != parentType)
                {
                    level++;
                    subclassType = subclassType.BaseType;

                    if (subclassType == null)
                        break;
                }

                if (subclassType == null)
                    continue;

                if (!arranged.ContainsKey(level))
                    arranged[level] = new List<IIndeterminateSubclassMappingProvider>();

                arranged[level].Add(provider);
            }

            return arranged;
        }
    }
}