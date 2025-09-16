// ملف جديد: WordTemplateRenderer.cs
using System;
using System.Collections.Generic;
using System.IO;
using Word = Microsoft.Office.Interop.Word;

public static class WordTemplateRenderer
{
    public static string RenderToHtml(string templatePath, Dictionary<string, string> vars, string outputFolder)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Template not found", templatePath);

        Directory.CreateDirectory(outputFolder);

        var app = new Word.Application();
        Word.Document doc = null;
        string outHtml = Path.Combine(outputFolder, $"preview_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        try
        {
            object readOnly = false, isVisible = false;
            object fileName = templatePath;

            doc = app.Documents.Open(ref fileName, ReadOnly: ref readOnly, Visible: ref isVisible);
            app.Visible = false;

            // استبدال المتغيّرات: يدعم {{NAME}} أو NAME مباشرةً
            foreach (var kv in vars)
            {
                ReplaceEverywhere(doc, "{{" + kv.Key + "}}", kv.Value ?? "");
                ReplaceEverywhere(doc, kv.Key, kv.Value ?? "");
            }

            object saveName = outHtml;
            object fmt = Word.WdSaveFormat.wdFormatFilteredHTML; // HTML
            doc.SaveAs2(ref saveName, ref fmt);

            return outHtml;
        }
        finally
        {
            if (doc != null) { doc.Close(false); System.Runtime.InteropServices.Marshal.ReleaseComObject(doc); }
            if (app != null) { app.Quit(); System.Runtime.InteropServices.Marshal.ReleaseComObject(app); }
        }
    }
    private static void ReplaceEverywhere(Word.Document doc, string findText, string replaceText)
    {
        if (string.IsNullOrEmpty(findText)) return;

        // مر على جميع قصص المستند: المتن، الرؤوس/التذييلات، مربعات النص... إلخ
        foreach (Word.Range story in doc.StoryRanges)
        {
            DoFindReplaceOnRange(story, findText, replaceText);

            // بعض القصص مرتبطة بسلاسل لاحقة
            Word.Range next = story.NextStoryRange;
            while (next != null)
            {
                DoFindReplaceOnRange(next, findText, replaceText);
                next = next.NextStoryRange;
            }
        }
    }

    private static void DoFindReplaceOnRange(Word.Range range, string findText, string replaceText)
    {
        Word.Find find = range.Find;
        find.ClearFormatting();
        find.Replacement.ClearFormatting();

        object replace = Word.WdReplace.wdReplaceAll;
        find.Execute(
            FindText: findText,
            MatchCase: false,
            MatchWholeWord: false,
            MatchWildcards: false,               // حتى لا تُفسَّر الأقواس المعقوفة كـ wildcards
            MatchSoundsLike: Type.Missing,
            MatchAllWordForms: false,
            Forward: true,
            Wrap: Word.WdFindWrap.wdFindContinue,
            Format: false,
            ReplaceWith: replaceText,
            Replace: replace
        );
    }


    private static void FindReplaceAll(Word.Application app, string findText, string replaceText)
    {
        if (string.IsNullOrEmpty(findText)) return;

        var find = app.Selection.Find;
        find.ClearFormatting();
        find.Text = findText;
        find.Replacement.ClearFormatting();
        find.Replacement.Text = replaceText;

        object replace = Word.WdReplace.wdReplaceAll;
        find.Execute(FindText: findText,
                     MatchCase: false,
                     MatchWholeWord: false,
                     MatchWildcards: false,
                     MatchSoundsLike: Type.Missing,
                     MatchAllWordForms: false,
                     Forward: true,
                     Wrap: Word.WdFindWrap.wdFindContinue,
                     Format: false,
                     ReplaceWith: replaceText,
                     Replace: replace);
    }
}
