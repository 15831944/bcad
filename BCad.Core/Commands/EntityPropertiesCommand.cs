using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using BCad.Entities;
using BCad.Services;

namespace BCad.Commands
{
    [ExportCommand("View.Properties", "properties", "prop", "p")]
    internal class EntityPropertiesCommand : ICommand
    {
        [Import]
        private IInputService InputService = null;

        public bool Execute(object arg = null)
        {
            var entity = InputService.GetEntity(new UserDirective("Select entity"));
            if (!entity.HasValue || entity.Cancel)
                return false;

            IEnumerable<string> properties;
            switch (entity.Value.Entity.Kind)
            {
                case EntityKind.Arc:
                    properties = new[] { "Center", "Color", "StartAngle", "EndAngle", "Normal", "Radius" };
                    break;
                case EntityKind.Circle:
                    properties = new[] { "Center", "Color", "Normal", "Radius" };
                    break;
                case EntityKind.Ellipse:
                    properties = new[] { "Center", "Color", "Normal", "MajorAxis", "MinorAxisRatio", "StartAngle", "EndAngle" };
                    break;
                case EntityKind.Line:
                    properties = new[] { "Color", "P1", "P2" };
                    break;
                case EntityKind.Polyline:
                    properties = new[] { "Color", "Points" };
                    break;
                case EntityKind.Text:
                    properties = new[] { "Color", "Height", "Location", "Normal", "Rotation", "Value" };
                    break;
                default:
                    throw new ArgumentException("Entity.Kind");
            }

            var details = DetailsFromProperties(entity.Value.Entity, properties);
            InputService.WriteLine(details);

            return true;
        }

        private static string DetailsFromProperties(Entity entity, IEnumerable<string> properties)
        {
            var type = entity.GetType();
            var details = new[] { "Kind", "Id" }.Concat(properties).Select(x =>
            {
                var prop = type.GetProperty(x);
                var value = prop.GetValue(entity, null);
                return string.Format("{0}: {1}", x, value);
            });
            return string.Join("\n", details);
        }

        public string DisplayName
        {
            get { return "PROPERTIES"; }
        }
    }
}
