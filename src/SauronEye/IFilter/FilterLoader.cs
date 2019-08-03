using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;

namespace EPocalipse.IFilter
{
  /// <summary>
  /// FilterLoader finds the dll and ClassID of the COM object responsible  
  /// for filtering a specific file extension. 
  /// It then loads that dll, creates the appropriate COM object and returns 
  /// a pointer to an IFilter instance
  /// </summary>
  static class FilterLoader
  {
    #region CacheEntry
    private class CacheEntry
    {
      public string DllName;
      public string ClassName;

      public CacheEntry(string dllName, string className)
      {
        DllName=dllName;
        ClassName=className;
      }
    }
    #endregion

    static Dictionary<string, CacheEntry> _cache=new Dictionary<string, CacheEntry>();

    #region Registry Read String helper
    static string ReadStrFromHKLM(string key)
    {
      return ReadStrFromHKLM(key,null);
    }
    static string ReadStrFromHKLM(string key, string value)
    {
      RegistryKey rk=Registry.LocalMachine.OpenSubKey(key);
      if (rk==null)
        return null;

      using (rk)
      {
        return (string)rk.GetValue(value);
      }
    }
    #endregion

    /// <summary>
    /// finds an IFilter implementation for a file type
    /// </summary>
    /// <param name="ext">The extension of the file</param>
    /// <returns>an IFilter instance used to retreive text from that file type</returns>
    private static IFilter LoadIFilter(string ext)
    {
      string dllName, filterPersistClass;

      //Find the dll and ClassID
      if (GetFilterDllAndClass(ext, out dllName, out filterPersistClass))
      {
        //load the dll and return an IFilter instance.
        return LoadFilterFromDll(dllName, filterPersistClass);
      }
      return null;
    }

    internal static IFilter LoadAndInitIFilter(string fileName)
    {
      return LoadAndInitIFilter(fileName,Path.GetExtension(fileName));
    }

    internal static IFilter LoadAndInitIFilter(string fileName, string extension)
    {
      IFilter filter=LoadIFilter(extension);
      if (filter==null)
        return null;

      IPersistFile persistFile=(filter as IPersistFile);
      if (persistFile!=null)
      {
        persistFile.Load(fileName, 0);
        IFILTER_FLAGS flags;
        IFILTER_INIT iflags =
					IFILTER_INIT.CANON_HYPHENS |
					IFILTER_INIT.CANON_PARAGRAPHS |
					IFILTER_INIT.CANON_SPACES |
					IFILTER_INIT.APPLY_INDEX_ATTRIBUTES |
					IFILTER_INIT.HARD_LINE_BREAKS |
					IFILTER_INIT.FILTER_OWNED_VALUE_OK;

        if (filter.Init(iflags, 0, IntPtr.Zero, out flags)==IFilterReturnCode.S_OK)
          return filter;
      }
      //If we failed to retreive an IPersistFile interface or to initialize 
      //the filter, we release it and return null.
      Marshal.ReleaseComObject(filter);
      return null;
    }

    private static IFilter LoadFilterFromDll(string dllName, string filterPersistClass)
    {
      //Get a classFactory for our classID
      IClassFactory classFactory=ComHelper.GetClassFactory(dllName, filterPersistClass);
      if (classFactory==null)
        return null;

      //And create an IFilter instance using that class factory
      Guid IFilterGUID=new Guid("89BCB740-6119-101A-BCB7-00DD010655AF");
      Object obj;
      classFactory.CreateInstance(null, ref IFilterGUID, out obj);
      return (obj as IFilter);
    }

    private static bool GetFilterDllAndClass(string ext, out string dllName, out string filterPersistClass)
    {
      if (!GetFilterDllAndClassFromCache(ext, out dllName, out filterPersistClass))
      {
        string persistentHandlerClass;

        persistentHandlerClass=GetPersistentHandlerClass(ext,true);
        if (persistentHandlerClass!=null)
        {
          GetFilterDllAndClassFromPersistentHandler(persistentHandlerClass,
            out dllName, out filterPersistClass);
        }
        AddExtensionToCache(ext, dllName, filterPersistClass);
      }
      return (dllName!=null && filterPersistClass!=null); 
    }

    private static void AddExtensionToCache(string ext, string dllName, string filterPersistClass)
    {
      lock (_cache)
      {
        _cache.Add(ext.ToLower(), new CacheEntry(dllName, filterPersistClass));
      }
    }

    private static bool GetFilterDllAndClassFromPersistentHandler(string persistentHandlerClass, out string dllName, out string filterPersistClass)
    {
      dllName=null;
      filterPersistClass=null;

      //Read the CLASS ID of the IFilter persistent handler
      filterPersistClass=ReadStrFromHKLM(@"Software\Classes\CLSID\" + persistentHandlerClass + 
        @"\PersistentAddinsRegistered\{89BCB740-6119-101A-BCB7-00DD010655AF}");
      if (String.IsNullOrEmpty(filterPersistClass))
          return false;

      //Read the dll name 
      dllName=ReadStrFromHKLM(@"Software\Classes\CLSID\" + filterPersistClass + 
        @"\InprocServer32");
      return (!String.IsNullOrEmpty(dllName));
    }

    private static string GetPersistentHandlerClass(string ext, bool searchContentType)
    {
      //Try getting the info from the file extension
      string persistentHandlerClass=GetPersistentHandlerClassFromExtension(ext);
      if (String.IsNullOrEmpty(persistentHandlerClass))
        //try getting the info from the document type 
        persistentHandlerClass=GetPersistentHandlerClassFromDocumentType(ext);
      if (searchContentType && String.IsNullOrEmpty(persistentHandlerClass))
        //Try getting the info from the Content Type
        persistentHandlerClass=GetPersistentHandlerClassFromContentType(ext);
      return persistentHandlerClass;
    }

    private static string GetPersistentHandlerClassFromContentType(string ext)
    {
      string contentType=ReadStrFromHKLM(@"Software\Classes\"+ext,"Content Type");
      if (String.IsNullOrEmpty(contentType))
        return null;
      
      string contentTypeExtension=ReadStrFromHKLM(@"Software\Classes\MIME\Database\Content Type\"+contentType,
          "Extension");
      if (ext.Equals(contentTypeExtension, StringComparison.CurrentCultureIgnoreCase))
        return null; //No need to look further. This extension does not have any persistent handler
    
      //We know the extension that is assciated with that content type. Simply try again with the new extension
      return GetPersistentHandlerClass(contentTypeExtension, false); //Don't search content type this time.
    }

    private static string GetPersistentHandlerClassFromDocumentType(string ext)
    {
      //Get the DocumentType of this file extension
      string docType=ReadStrFromHKLM(@"Software\Classes\"+ext);
      if (String.IsNullOrEmpty(docType))
        return null;
      
      //Get the Class ID for this document type
      string docClass=ReadStrFromHKLM(@"Software\Classes\" + docType + @"\CLSID");
      if (String.IsNullOrEmpty(docType))
        return null;

      //Now get the PersistentHandler for that Class ID
      return ReadStrFromHKLM(@"Software\Classes\CLSID\" + docClass + @"\PersistentHandler");
    }

    private static string GetPersistentHandlerClassFromExtension(string ext)
    {
      return ReadStrFromHKLM(@"Software\Classes\"+ext+@"\PersistentHandler");
    }

    private static bool GetFilterDllAndClassFromCache(string ext, out string dllName, out string filterPersistClass)
    {
      string lowerExt=ext.ToLower();
      lock (_cache)
      {
        CacheEntry cacheEntry;
        if (_cache.TryGetValue(lowerExt, out cacheEntry))
        {
          dllName=cacheEntry.DllName;
          filterPersistClass=cacheEntry.ClassName;
          return true;
        }
      }
      dllName=null;
      filterPersistClass=null;
      return false;
    }
  }
}
