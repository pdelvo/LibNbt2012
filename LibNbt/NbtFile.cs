﻿using System;
using System.IO;
using System.IO.Compression;
using JetBrains.Annotations;

namespace LibNbt {
    /// <summary> Represents a complete NBT file. </summary>
    public sealed class NbtFile {
        // buffer used to avoid frequent reads from / writes to compressed streams
        const int BufferSize = 8192;

        /// <summary> Gets the file name used for most recent loading/saving of this file.
        /// May be null, if this NbtFile instance has not been loaded from, or saved to, a file. </summary>
        [CanBeNull]
        public string FileName { get; private set; }


        /// <summary> Gets the compression method used for most recent loading/saving of this file.
        /// Defaults to AutoDetect. </summary>
        public NbtCompression FileCompression { get; private set; }


        /// <summary> Root tag of this file. Must be a named CompoundTag. Defaults to null. </summary>
        /// <exception cref="ArgumentException"> If given tag is unnamed. </exception>
        [CanBeNull]
        public NbtCompound RootTag {
            get { return rootTag; }
            set {
                if( value != null && value.Name == null ) {
                    throw new ArgumentException( "Root tag must be named." );
                }
                rootTag = value;
            }
        }

        NbtCompound rootTag;


        /// <summary> Creates a new NBT file with the given root tag. </summary>
        /// <param name="rootTag"> Compound tag to set as the root tag. May be null. </param>
        /// <exception cref="ArgumentException"> If given rootTag is unnamed. </exception>
        public NbtFile( NbtCompound rootTag ) {
            RootTag = rootTag;
        }


        /// <summary> Loads NBT data from a file. Compression will be auto-detected. </summary>
        /// <param name="fileName"> Name of the file from which data will be loaded. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for compression. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while reading the file. </exception>
        public NbtFile( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            LoadFromFile( fileName, NbtCompression.AutoDetect );
        }


        /// <summary> Loads NBT data from a file. </summary>
        /// <param name="fileName"> Name of the file from which data will be loaded. </param>
        /// <param name="compression"> Compression method to use for loading/saving this file. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for compression. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while reading the file. </exception>
        public NbtFile( [NotNull] string fileName, NbtCompression compression ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            LoadFromFile( fileName, compression );
        }


        /// <summary> Loads NBT data from a stream. </summary>
        /// <param name="stream"> Stream from which data will be loaded. If compression is set to AutoDetect, this stream must support seeking. </param>
        /// <param name="compression"> Compression method to use for loading/saving this file. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="stream"/> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for compression. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="NotSupportedException"> If compression is set to AutoDetect, but the stream is not seekable. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, decompressing failed, or given stream does not support reading. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        public NbtFile( [NotNull] Stream stream, NbtCompression compression ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            LoadFromStream( stream, compression );
        }


        /// <summary> Loads NBT data from a file. Existing RootTag will be replaced. Compression will be auto-detected. </summary>
        /// <param name="fileName"> Name of the file from which data will be loaded. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for compression. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while reading the file. </exception>
        public void LoadFromFile( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            LoadFromFile( fileName, NbtCompression.AutoDetect );
        }


        /// <summary> Loads NBT data from a file. Existing RootTag will be replaced. </summary>
        /// <param name="fileName"> Name of the file from which data will be loaded. </param>
        /// <param name="compression"> Compression method to use for loading/saving this file. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for compression. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while reading the file. </exception>
        public void LoadFromFile( [NotNull] string fileName, NbtCompression compression ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( !File.Exists( fileName ) ) {
                throw new FileNotFoundException( String.Format( "Could not find NBT file: {0}", fileName ),
                                                 fileName );
            }

            using( FileStream readFileStream = File.OpenRead( fileName ) ) {
                LoadFromStream( readFileStream, compression );
            }
            FileName = fileName;
        }


        /// <summary> Loads NBT data from a stream. Existing RootTag will be replaced </summary>
        /// <param name="stream"> Stream from which data will be loaded. If compression is set to AutoDetect, this stream must support seeking. </param>
        /// <param name="compression"> Compression method to use for loading/saving this file. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="stream"/> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for compression. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="NotSupportedException"> If compression is set to AutoDetect, but the stream is not seekable. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, decompressing failed, or given stream does not support reading. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        public void LoadFromStream( [NotNull] Stream stream, NbtCompression compression ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );

            FileName = null;
            FileCompression = compression;

            // detect compression, based on the first byte
            if( compression == NbtCompression.AutoDetect ) {
                if( !stream.CanSeek ) {
                    throw new NotSupportedException( "Cannot auto-detect compression on a stream that's not seekable." );
                }
                int firstByte = stream.ReadByte();
                switch( firstByte ) {
                    case -1:
                        throw new EndOfStreamException();

                    case (byte)NbtTagType.Compound: // 0x0A
                        compression = NbtCompression.None;
                        break;

                    case 0x1F:
                        // gzip magic number
                        compression = NbtCompression.GZip;
                        break;

                    case 0x78:
                        // zlib header
                        compression = NbtCompression.ZLib;
                        break;
                        
                    default:
                        throw new InvalidDataException( "Could not auto-detect compression format." );
                }
                stream.Seek( -1, SeekOrigin.Current );
            }

            switch( compression ) {
                case NbtCompression.GZip:
                    using( var decStream = new GZipStream( stream, CompressionMode.Decompress, true ) ) {
                        LoadFromStreamInternal( new BufferedStream( decStream, BufferSize ) );
                    }
                    break;

                case NbtCompression.None:
                    LoadFromStreamInternal( stream );
                    break;

                case NbtCompression.ZLib:
                    if( stream.ReadByte() != 0x78 ) {
                        throw new InvalidDataException( "Incorrect ZLib header. Expected 0x78 0x9C" );
                    }
                    stream.ReadByte();
                    using( var decStream = new DeflateStream( stream, CompressionMode.Decompress, true ) ) {
                        LoadFromStreamInternal( new BufferedStream( decStream, BufferSize ) );
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException( "compression" );
            }
        }


        void LoadFromStreamInternal( [NotNull] Stream fileStream ) {
            if( fileStream == null ) throw new ArgumentNullException( "fileStream" );

            // Make sure the first byte in this file is the tag for a TAG_Compound
            if( fileStream.ReadByte() != (int)NbtTagType.Compound ) {
                throw new NbtFormatException( "File format does not start with a TAG_Compound" );
            }

            var rootCompound = new NbtCompound();
            rootCompound.ReadTag( new NbtReader( fileStream ), true );
            RootTag = rootCompound;
        }


        /// <summary> Saves this NBT file to a stream. Nothing is written to stream if RootTag is null. </summary>
        /// <param name="fileName"> File to write data to. May not be null. </param>
        /// <param name="compression"> Compression mode to use for saving. May not be AutoDetect. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is null. </exception>
        /// <exception cref="ArgumentException"> If AutoDetect was given as the compression mode. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for compression. </exception>
        /// <exception cref="InvalidDataException"> If given stream does not support writing. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while creating the file. </exception>
        /// <exception cref="UnauthorizedAccessException"> Specified file is read-only, or a permission issue occurred. </exception>
        /// <exception cref="NbtFormatException"> If one of the NbtCompound tags contained unnamed tags;
        /// or if an NbtList tag had Unknown list type and no elements. </exception>
        public void SaveToFile( [NotNull] string fileName, NbtCompression compression ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );

            using( FileStream saveFile = File.Create( fileName ) ) {
                SaveToStream( saveFile, compression );
            }
        }


        /// <summary> Saves this NBT file to a stream. Nothing is written to stream if RootTag is null. </summary>
        /// <param name="stream"> Stream to write data to. May not be null. </param>
        /// <param name="compression"> Compression mode to use for saving. May not be AutoDetect. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="stream"/> is null. </exception>
        /// <exception cref="ArgumentException"> If AutoDetect was given as the compression mode. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for compression. </exception>
        /// <exception cref="InvalidDataException"> If given stream does not support writing. </exception>
        /// <exception cref="NbtFormatException"> If one of the NbtCompound tags contained unnamed tags;
        /// or if an NbtList tag had Unknown list type and no elements. </exception>
        public void SaveToStream( [NotNull] Stream stream, NbtCompression compression ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );

            switch( compression ) {
                case NbtCompression.AutoDetect:
                    throw new ArgumentException( "AutoDetect is not a valid NbtCompression value for saving." );
                case NbtCompression.ZLib:
                case NbtCompression.GZip:
                case NbtCompression.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException( "compression" );
            }

            // do not write anything for empty tags
            if( RootTag == null ) return;

            switch( compression ) {
                case NbtCompression.ZLib:
                    stream.WriteByte( 0x78 );
                    stream.WriteByte( 0x01 );
                    int checksum;
                    using( var compressStream = new ZLibStream( stream, CompressionMode.Compress, true ) ) {
                        BufferedStream bufferedStream = new BufferedStream( compressStream, BufferSize );
                        RootTag.WriteTag( new NbtWriter( bufferedStream ), true );
                        bufferedStream.Flush();
                        checksum = compressStream.Checksum;
                    }
                    byte[] checksumBytes = BitConverter.GetBytes(checksum);
                    if( BitConverter.IsLittleEndian ) {
                        // Adler32 checksum is big-endian
                        Array.Reverse( checksumBytes );
                    }
                    stream.Write( checksumBytes, 0, checksumBytes.Length );
                    break;

                case NbtCompression.GZip:
                    using( var compressStream = new GZipStream( stream, CompressionMode.Compress, true ) ) {
                        // use a buffered stream to avoid gzipping in small increments (which has a lot of overhead)
                        BufferedStream bufferedStream = new BufferedStream( compressStream, BufferSize );
                        RootTag.WriteTag( new NbtWriter( bufferedStream ), true );
                        bufferedStream.Flush();
                    }
                    break;

                case NbtCompression.None:
                    RootTag.WriteTag( new NbtWriter( stream ), true );
                    break;
            }
        }
    }
}