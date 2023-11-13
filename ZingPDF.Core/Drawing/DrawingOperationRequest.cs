using System.ComponentModel;
using ZingPdf.Core.Validation;

namespace ZingPdf.Core.Drawing
{
    public class DrawingOperationRequest
    {
        [Obsolete("Reserved for deserialisation")]
        public DrawingOperationRequest() { }

        public DrawingOperationRequest(CoordinateSystem coordinateSystem, IEnumerable<PathOperation> pathOperations) : this(data:null, coordinateSystem, pathOperations, null, null) { }
        public DrawingOperationRequest(CoordinateSystem coordinateSystem, IEnumerable<TextOperation> textOperations) : this(data: null, coordinateSystem, null, textOperations, null) { }
        public DrawingOperationRequest(CoordinateSystem coordinateSystem, IEnumerable<ImageOperation> imageOperations) : this(data: null, coordinateSystem, null, null, imageOperations) { }

        public DrawingOperationRequest(byte[] data, CoordinateSystem coordinateSystem, IEnumerable<PathOperation> pathOperations) : this(data, coordinateSystem, pathOperations, null, null) { }
        public DrawingOperationRequest(byte[] data, CoordinateSystem coordinateSystem, IEnumerable<TextOperation> textOperations) : this(data, coordinateSystem, null, textOperations, null) { }
        public DrawingOperationRequest(byte[] data, CoordinateSystem coordinateSystem, IEnumerable<ImageOperation> imageOperations) : this(data, coordinateSystem, null, null, imageOperations) { }

        public DrawingOperationRequest(CoordinateSystem coordinateSystem, IEnumerable<PathOperation> pathOperations, IEnumerable<TextOperation> textOperations) : this(data:null, coordinateSystem, pathOperations, textOperations, null) { }
        public DrawingOperationRequest(CoordinateSystem coordinateSystem, IEnumerable<PathOperation> pathOperations, IEnumerable<ImageOperation> imageOperations) : this(data: null, coordinateSystem, pathOperations, null, imageOperations) { }

        public DrawingOperationRequest(CoordinateSystem coordinateSystem, IEnumerable<TextOperation> textOperations, IEnumerable<ImageOperation> imageOperations) : this(data: null, coordinateSystem, null, textOperations, imageOperations) { }

        public DrawingOperationRequest(byte[] data, CoordinateSystem coordinateSystem, IEnumerable<PathOperation> pathOperations, IEnumerable<TextOperation> textOperations) : this(data, coordinateSystem, pathOperations, textOperations, null) { }
        public DrawingOperationRequest(byte[] data, CoordinateSystem coordinateSystem, IEnumerable<PathOperation> pathOperations, IEnumerable<ImageOperation> imageOperations) : this(data, coordinateSystem, pathOperations, null, imageOperations) { }
        public DrawingOperationRequest(byte[] data, CoordinateSystem coordinateSystem, IEnumerable<TextOperation> textOperations, IEnumerable<ImageOperation> imageOperations) : this(data, coordinateSystem, null, textOperations, imageOperations) { }

        public DrawingOperationRequest(string url, CoordinateSystem coordinateSystem, IEnumerable<PathOperation> pathOperations) : this(url, coordinateSystem, pathOperations, null, null) { }
        public DrawingOperationRequest(string url, CoordinateSystem coordinateSystem, IEnumerable<TextOperation> textOperations) : this(url, coordinateSystem, null, textOperations, null) { }
        public DrawingOperationRequest(string url, CoordinateSystem coordinateSystem, IEnumerable<ImageOperation> imageOperations) : this(url, coordinateSystem, null, null, imageOperations) { }

        public DrawingOperationRequest(string url, CoordinateSystem coordinateSystem, IEnumerable<PathOperation> pathOperations, IEnumerable<TextOperation> textOperations) : this(url, coordinateSystem, pathOperations, textOperations, null) { }
        public DrawingOperationRequest(string url, CoordinateSystem coordinateSystem, IEnumerable<PathOperation> pathOperations, IEnumerable<ImageOperation> imageOperations) : this(url, coordinateSystem, pathOperations, null, imageOperations) { }
        public DrawingOperationRequest(string url, CoordinateSystem coordinateSystem, IEnumerable<TextOperation> textOperations, IEnumerable<ImageOperation> imageOperations) : this(url, coordinateSystem, null, textOperations, imageOperations) { }

        public DrawingOperationRequest(
            byte[] data,
            CoordinateSystem coordinateSystem,
            IEnumerable<PathOperation> pathOperations,
            IEnumerable<TextOperation> textOperations,
            IEnumerable<ImageOperation> imageOperations
            )
        {
            if (!Enum.IsDefined(typeof(CoordinateSystem), coordinateSystem)) throw new InvalidEnumArgumentException(nameof(coordinateSystem), (int)coordinateSystem, typeof(CoordinateSystem));

            Data = data;
            CoordinateSystem = coordinateSystem;
            PathOperations = pathOperations;
            TextOperations = textOperations;
            ImageOperations = imageOperations;

            if (pathOperations?.Any(p => p == null) ?? false) throw new ArgumentException("Null value encountered in collection argument", nameof(pathOperations));
            if (textOperations?.Any(t => t == null) ?? false) throw new ArgumentException("Null value encountered in collection argument", nameof(textOperations));
            if (imageOperations?.Any(t => t == null) ?? false) throw new ArgumentException("Null value encountered in collection argument", nameof(imageOperations));
        }

        public DrawingOperationRequest(
            string url,
            CoordinateSystem coordinateSystem,
            IEnumerable<PathOperation> pathOperations,
            IEnumerable<TextOperation> textOperations,
            IEnumerable<ImageOperation> imageOperations
        )
        {
            if (!Enum.IsDefined(typeof(CoordinateSystem), coordinateSystem)) throw new InvalidEnumArgumentException(nameof(coordinateSystem), (int)coordinateSystem, typeof(CoordinateSystem));
            
            Url = url;
            CoordinateSystem = coordinateSystem;
            PathOperations = pathOperations;
            TextOperations = textOperations;
            ImageOperations = imageOperations;

            if (pathOperations?.Any(p => p == null) ?? false) throw new ArgumentException("Null value encountered in collection argument", nameof(pathOperations));
            if (textOperations?.Any(t => t == null) ?? false) throw new ArgumentException("Null value encountered in collection argument", nameof(textOperations));
            if (imageOperations?.Any(t => t == null) ?? false) throw new ArgumentException("Null value encountered in collection argument", nameof(imageOperations));
        }

        /// <summary>
        /// Optional PDF data onto which the drawing operations will be overlaid.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Optional url of which the file can be fetched
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Specifies the origin of the coordinate system.
        /// </summary>
        public CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.BottomUp;

        /// <summary>
        /// A collection of operations for drawing paths.
        /// </summary>
        [RequiredIfNull(nameof(TextOperations), nameof(ImageOperations), ErrorMessage = "One of PathOperations, TextOperations, or ImageOperations is required")]
        public IEnumerable<PathOperation> PathOperations { get; set; }

        /// <summary>
        /// A collection of operations for drawing text.
        /// </summary>
        [RequiredIfNull(nameof(PathOperations), nameof(ImageOperations), ErrorMessage = "One of PathOperations, TextOperations, or ImageOperations is required")]
        public IEnumerable<TextOperation> TextOperations { get; set; }

        /// <summary>
        /// A collection of operations for drawing images.
        /// </summary>
        [RequiredIfNull(nameof(PathOperations), nameof(TextOperations), ErrorMessage = "One of PathOperations, TextOperations, or ImageOperations is required")]
        public IEnumerable<ImageOperation> ImageOperations { get; set; }
    }
}
