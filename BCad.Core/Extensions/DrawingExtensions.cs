﻿using BCad.Entities;

namespace BCad
{
    public static class DrawingExtensions
    {
        /// <summary>
        /// Adds an entity to the specified layer.
        /// </summary>
        /// <param name="layer">The layer to which to add the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The new drawing with the layer added.</returns>
        public static Drawing Add(this Drawing drawing, Layer layer, Entity entity)
        {
            var updatedLayer = layer.Add(entity);
            return drawing.Replace(layer, updatedLayer);
        }

        /// <summary>
        /// Replaces the specified entity.
        /// </summary>
        /// <param name="oldEntity">The entity to be replaced.</param>
        /// <param name="newEntity">The replacement entity.</param>
        /// <returns>The new drawing with the entity replaced.</returns>
        public static Drawing Replace(this Drawing drawing, Entity oldEntity, Entity newEntity)
        {
            var layer = drawing.ContainingLayer(oldEntity);
            if (layer == null)
            {
                return drawing;
            }

            return drawing.Replace(layer, oldEntity, newEntity);
        }

        /// <summary>
        /// Replaces the entity in the specified layer.
        /// </summary>
        /// <param name="layer">The layer containing the entity.</param>
        /// <param name="oldEntity">The entity to be replaced.</param>
        /// <param name="newEntity">The replacement entity.</param>
        /// <returns>The new drawing with the entity replaced.</returns>
        public static Drawing Replace(this Drawing drawing, Layer layer, Entity oldEntity, Entity newEntity)
        {
            var updatedLayer = layer.Replace(oldEntity, newEntity);
            return drawing.Replace(layer, updatedLayer);
        }


        /// <summary>
        /// Removes the entity from the layer.
        /// </summary>
        /// <param name="layer">The containing layer.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <returns>The new drawing with the entity removed.</returns>
        public static Drawing Remove(this Drawing drawing, Layer layer, Entity entity)
        {
            var updatedLayer = layer.Remove(entity);
            return drawing.Replace(layer, updatedLayer);
        }

        /// <summary>
        /// Removes the entity from the drawing.
        /// </summary>
        /// <param name="drawing">The drawing.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <returns>The new drawing with the entity removed.</returns>
        public static Drawing Remove(this Drawing drawing, Entity entity)
        {
            var layer = drawing.ContainingLayer(entity);
            if (layer != null)
            {
                return drawing.Remove(layer, entity);
            }

            return drawing;
        }

        /// <summary>
        /// Returns the layer that contains the specified entity.
        /// </summary>
        /// <param name="entity">The entity to find.</param>
        /// <returns>The containing layer.</returns>
        public static Layer ContainingLayer(this Drawing drawing, Entity entity)
        {
            foreach (var layer in drawing.Layers.Values)
            {
                if (layer.EntityExists(entity))
                    return layer;
            }

            return null;
        }
    }
}