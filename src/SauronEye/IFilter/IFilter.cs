using System;
using System.Text;
using System.Runtime.InteropServices;

//Contains IFilter interface translation
//Most translations are from PInvoke.net

namespace EPocalipse.IFilter
{
  [StructLayout(LayoutKind.Sequential)]
  public struct FULLPROPSPEC 
  {
    public Guid guidPropSet;
    public PROPSPEC psProperty;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct FILTERREGION 
  {
    public int idChunk;
    public int cwcStart;
    public int cwcExtent;
  }

  [StructLayout(LayoutKind.Explicit)]
  public struct PROPSPEC
  {
    [FieldOffset(0)] public int ulKind;     // 0 - string used; 1 - PROPID
    [FieldOffset(4)] public int propid;    
    [FieldOffset(4)] public IntPtr lpwstr;
  }

  [Flags]
  internal enum IFILTER_FLAGS 
  {
    /// <summary>
    /// The caller should use the IPropertySetStorage and IPropertyStorage
    /// interfaces to locate additional properties. 
    /// When this flag is set, properties available through COM
    /// enumerators should not be returned from IFilter. 
    /// </summary>
    IFILTER_FLAGS_OLE_PROPERTIES = 1
  }

  /// <summary>
  /// Flags controlling the operation of the FileFilter
  /// instance.
  /// </summary>
  [Flags]
  internal enum IFILTER_INIT
  {
    NONE = 0,
    /// <summary>
    /// Paragraph breaks should be marked with the Unicode PARAGRAPH
    /// SEPARATOR (0x2029)
    /// </summary>
    CANON_PARAGRAPHS = 1,

    /// <summary>
    /// Soft returns, such as the newline character in Microsoft Word, should
    /// be replaced by hard returnsLINE SEPARATOR (0x2028). Existing hard
    /// returns can be doubled. A carriage return (0x000D), line feed (0x000A),
    /// or the carriage return and line feed in combination should be considered
    /// a hard return. The intent is to enable pattern-expression matches that
    /// match against observed line breaks. 
    /// </summary>
    HARD_LINE_BREAKS = 2,

    /// <summary>
    /// Various word-processing programs have forms of hyphens that are not
    /// represented in the host character set, such as optional hyphens
    /// (appearing only at the end of a line) and nonbreaking hyphens. This flag
    /// indicates that optional hyphens are to be converted to nulls, and
    /// non-breaking hyphens are to be converted to normal hyphens (0x2010), or
    /// HYPHEN-MINUSES (0x002D). 
    /// </summary>
    CANON_HYPHENS = 4,

    /// <summary>
    /// Just as the CANON_HYPHENS flag standardizes hyphens,
    /// this one standardizes spaces. All special space characters, such as
    /// nonbreaking spaces, are converted to the standard space character
    /// (0x0020). 
    /// </summary>
    CANON_SPACES = 8,

    /// <summary>
    /// Indicates that the client wants text split into chunks representing
    /// public value-type properties. 
    /// </summary>
    APPLY_INDEX_ATTRIBUTES = 16,

    /// <summary>
    /// Indicates that the client wants text split into chunks representing
    /// properties determined during the indexing process. 
    /// </summary>
    APPLY_CRAWL_ATTRIBUTES = 256,

    /// <summary>
    /// Any properties not covered by the APPLY_INDEX_ATTRIBUTES
    /// and APPLY_CRAWL_ATTRIBUTES flags should be emitted. 
    /// </summary>
    APPLY_OTHER_ATTRIBUTES = 32,

    /// <summary>
    /// Optimizes IFilter for indexing because the client calls the
    /// IFilter::Init method only once and does not call IFilter::BindRegion.
    /// This eliminates the possibility of accessing a chunk both before and
    /// after accessing another chunk. 
    /// </summary>
    INDEXING_ONLY = 64,

    /// <summary>
    /// The text extraction process must recursively search all linked
    /// objects within the document. If a link is unavailable, the
    /// IFilter::GetChunk call that would have obtained the first chunk of the
    /// link should return FILTER_E_LINK_UNAVAILABLE. 
    /// </summary>
    SEARCH_LINKS = 128,

    /// <summary>
    /// The content indexing process can return property values set by the  filter. 
    /// </summary>
    FILTER_OWNED_VALUE_OK = 512
  }

  public struct STAT_CHUNK 
  {
    /// <summary>
    /// The chunk identifier. Chunk identifiers must be unique for the
    /// current instance of the IFilter interface. 
    /// Chunk identifiers must be in ascending order. The order in which
    /// chunks are numbered should correspond to the order in which they appear
    /// in the source document. Some search engines can take advantage of the
    /// proximity of chunks of various properties. If so, the order in which
    /// chunks with different properties are emitted will be important to the
    /// search engine. 
    /// </summary>
    public int idChunk;

    /// <summary>
    /// The type of break that separates the previous chunk from the current
    ///  chunk. Values are from the CHUNK_BREAKTYPE enumeration. 
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public CHUNK_BREAKTYPE breakType;

    /// <summary>
    /// Flags indicate whether this chunk contains a text-type or a
    /// value-type property. 
    /// Flag values are taken from the CHUNKSTATE enumeration. If the CHUNK_TEXT flag is set, 
    /// IFilter::GetText should be used to retrieve the contents of the chunk
    /// as a series of words. 
    /// If the CHUNK_VALUE flag is set, IFilter::GetValue should be used to retrieve 
    /// the value and treat it as a single property value. If the filter dictates that the same 
    /// content be treated as both text and as a value, the chunk should be emitted twice in two       
    /// different chunks, each with one flag set. 
    /// </summary>
    [MarshalAs(UnmanagedType.U4)]
    public CHUNKSTATE flags;

    /// <summary>
    /// The language and sublanguage associated with a chunk of text. Chunk locale is used 
    /// by document indexers to perform proper word breaking of text. If the chunk is 
    /// neither text-type nor a value-type with data type VT_LPWSTR, VT_LPSTR or VT_BSTR, 
    /// this field is ignored. 
    /// </summary>
    public int locale;

    /// <summary>
    /// The property to be applied to the chunk. If a filter requires that       the same text 
    /// have more than one property, it needs to emit the text once for each       property 
    /// in separate chunks. 
    /// </summary>
    public FULLPROPSPEC attribute;

    /// <summary>
    /// The ID of the source of a chunk. The value of the idChunkSource     member depends on the nature of the chunk: 
    /// If the chunk is a text-type property, the value of the idChunkSource       member must be the same as the value of the idChunk member. 
    /// If the chunk is an public value-type property derived from textual       content, the value of the idChunkSource member is the chunk ID for the
    /// text-type chunk from which it is derived. 
    /// If the filter attributes specify to return only public value-type
    /// properties, there is no content chunk from which to derive the current
    /// public value-type property. In this case, the value of the
    /// idChunkSource member must be set to zero, which is an invalid chunk. 
    /// </summary>
    public int idChunkSource;

    /// <summary>
    /// The offset from which the source text for a derived chunk starts in
    /// the source chunk. 
    /// </summary>
    public int cwcStartSource;

    /// <summary>
    /// The length in characters of the source text from which the current
    /// chunk was derived. 
    /// A zero value signifies character-by-character correspondence between
    /// the source text and 
    /// the derived text. A nonzero value means that no such direct
    /// correspondence exists
    /// </summary>
    public int cwcLenSource;
  }

  /// <summary>
  /// Enumerates the different breaking types that occur between 
  /// chunks of text read out by the FileFilter.
  /// </summary>
  public enum CHUNK_BREAKTYPE
  {
    /// <summary>
    /// No break is placed between the current chunk and the previous chunk.
    /// The chunks are glued together. 
    /// </summary>
    CHUNK_NO_BREAK = 0,
    /// <summary>
    /// A word break is placed between this chunk and the previous chunk that
    /// had the same attribute. 
    /// Use of CHUNK_EOW should be minimized because the choice of word
    /// breaks is language-dependent, 
    /// so determining word breaks is best left to the search engine. 
    /// </summary>
    CHUNK_EOW = 1,
    /// <summary>
    /// A sentence break is placed between this chunk and the previous chunk
    /// that had the same attribute. 
    /// </summary>
    CHUNK_EOS = 2,
    /// <summary>
    /// A paragraph break is placed between this chunk and the previous chunk
    /// that had the same attribute.
    /// </summary>     
    CHUNK_EOP = 3,
    /// <summary>
    /// A chapter break is placed between this chunk and the previous chunk
    /// that had the same attribute. 
    /// </summary>
    CHUNK_EOC = 4
  }


  public enum CHUNKSTATE 
  {
    /// <summary>
    /// The current chunk is a text-type property.
    /// </summary>
    CHUNK_TEXT = 0x1,
    /// <summary>
    /// The current chunk is a value-type property. 
    /// </summary>
    CHUNK_VALUE = 0x2,
    /// <summary>
    /// Reserved
    /// </summary>
    CHUNK_FILTER_OWNED_VALUE = 0x4
  }

  internal enum IFilterReturnCode : uint 
  {
    /// <summary>
    /// Success
    /// </summary>
    S_OK = 0,
    /// <summary>
    /// The function was denied access to the filter file. 
    /// </summary>
    E_ACCESSDENIED = 0x80070005,
    /// <summary>
    /// The function encountered an invalid handle,
    /// probably due to a low-memory situation. 
    /// </summary>
    E_HANDLE = 0x80070006,
    /// <summary>
    /// The function received an invalid parameter.
    /// </summary>
    E_INVALIDARG = 0x80070057,
    /// <summary>
    /// Out of memory
    /// </summary>
    E_OUTOFMEMORY = 0x8007000E,
    /// <summary>
    /// Not implemented
    /// </summary>
    E_NOTIMPL = 0x80004001,
    /// <summary>
    /// Unknown error
    /// </summary>
    E_FAIL = 0x80000008,
    /// <summary>
    /// File not filtered due to password protection
    /// </summary>
    FILTER_E_PASSWORD = 0x8004170B,
    /// <summary>
    /// The document format is not recognised by the filter
    /// </summary>
    FILTER_E_UNKNOWNFORMAT = 0x8004170C,
    /// <summary>
    /// No text in current chunk
    /// </summary>
    FILTER_E_NO_TEXT = 0x80041705,
    /// <summary>
    /// No more chunks of text available in object
    /// </summary>
    FILTER_E_END_OF_CHUNKS = 0x80041700,
    /// <summary>
    /// No more text available in chunk
    /// </summary>
    FILTER_E_NO_MORE_TEXT = 0x80041701,
    /// <summary>
    /// No more property values available in chunk
    /// </summary>
    FILTER_E_NO_MORE_VALUES = 0x80041702,
    /// <summary>
    /// Unable to access object
    /// </summary>
    FILTER_E_ACCESS = 0x80041703,
    /// <summary>
    /// Moniker doesn't cover entire region
    /// </summary>
    FILTER_W_MONIKER_CLIPPED = 0x00041704,
    /// <summary>
    /// Unable to bind IFilter for embedded object
    /// </summary>
    FILTER_E_EMBEDDING_UNAVAILABLE = 0x80041707,
    /// <summary>
    /// Unable to bind IFilter for linked object
    /// </summary>
    FILTER_E_LINK_UNAVAILABLE = 0x80041708,
    /// <summary>
    ///  This is the last text in the current chunk
    /// </summary>
    FILTER_S_LAST_TEXT = 0x00041709,
    /// <summary>
    /// This is the last value in the current chunk
    /// </summary>
    FILTER_S_LAST_VALUES = 0x0004170A
  }

  [ComImport, Guid("89BCB740-6119-101A-BCB7-00DD010655AF")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IFilter
  {
    /// <summary>
    /// The IFilter::Init method initializes a filtering session.
    /// </summary>
    [PreserveSig]
    IFilterReturnCode Init(
      //[in] Flag settings from the IFILTER_INIT enumeration for
      // controlling text standardization, property output, embedding
      // scope, and IFilter access patterns. 
      IFILTER_INIT grfFlags,

      // [in] The size of the attributes array. When nonzero, cAttributes
      //  takes 
      // precedence over attributes specified in grfFlags. If no
      // attribute flags 
      // are specified and cAttributes is zero, the default is given by
      // the 
      // PSGUID_STORAGE storage property set, which contains the date and
      //  time 
      // of the last write to the file, size, and so on; and by the
      //  PID_STG_CONTENTS 
      // 'contents' property, which maps to the main contents of the
      // file. 
      // For more information about properties and property sets, see
      // Property Sets. 
      int cAttributes,

      //[in] Array of pointers to FULLPROPSPEC structures for the
      // requested properties. 
      // When cAttributes is nonzero, only the properties in aAttributes
      // are returned. 
      IntPtr aAttributes,

      // [out] Information about additional properties available to the
      //  caller; from the IFILTER_FLAGS enumeration. 
      out IFILTER_FLAGS pdwFlags);

    /// <summary>
    /// The IFilter::GetChunk method positions the filter at the beginning
    /// of the next chunk, 
    /// or at the first chunk if this is the first call to the GetChunk
    /// method, and returns a description of the current chunk. 
    /// </summary>
    [PreserveSig]
    IFilterReturnCode GetChunk(out STAT_CHUNK pStat);

    /// <summary>
    /// The IFilter::GetText method retrieves text (text-type properties)
    /// from the current chunk, 
    /// which must have a CHUNKSTATE enumeration value of CHUNK_TEXT.
    /// </summary>
    [PreserveSig]
    IFilterReturnCode GetText(
      // [in/out] On entry, the size of awcBuffer array in wide/Unicode
      // characters. On exit, the number of Unicode characters written to
      // awcBuffer. 
      // Note that this value is not the number of bytes in the buffer. 
      ref uint pcwcBuffer,

      // Text retrieved from the current chunk. Do not terminate the
      // buffer with a character.  
      [Out(), MarshalAs(UnmanagedType.LPArray)] 
      char[] awcBuffer);

    /// <summary>
    /// The IFilter::GetValue method retrieves a value (public
    /// value-type property) from a chunk, 
    /// which must have a CHUNKSTATE enumeration value of CHUNK_VALUE.
    /// </summary>
    [PreserveSig]
    int GetValue(
      // Allocate the PROPVARIANT structure with CoTaskMemAlloc. Some
      // PROPVARIANT 
      // structures contain pointers, which can be freed by calling the
      // PropVariantClear function. 
      // It is up to the caller of the GetValue method to call the
      //   PropVariantClear method.            
      // ref IntPtr ppPropValue
      // [MarshalAs(UnmanagedType.Struct)]
      ref IntPtr PropVal);

    /// <summary>
    /// The IFilter::BindRegion method retrieves an interface representing
    /// the specified portion of the object. 
    /// Currently reserved for future use.
    /// </summary>
    [PreserveSig]
    int BindRegion(ref FILTERREGION origPos,
      ref Guid riid, ref object ppunk);
  }


}
