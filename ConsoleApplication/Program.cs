using System;

namespace ConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(string.Format(KoreanFormatInfo.CurrentInfo, "{0:은/는} {1:(이)다}.", "대한민국", "민주공화국"));
            Console.WriteLine(string.Format(KoreanFormatInfo.CurrentInfo, "{0:은/는} {1:(이)다}.", "Korea", "republic"));

            Console.WriteLine(string.Format(KoreanFormatInfo.CurrentInfo, "{0}의 {1:은/는} {2}에게 있고 모든 {3:은/는} {2:(으)로}부터 나온다.", "대한민국", "주권", "국민", "권력"));
            Console.WriteLine(string.Format(KoreanFormatInfo.CurrentInfo, "{0}의 {1:은/는} {2}에게 있고 모든 {3:은/는} {2:(으)로}부터 나온다.", "Korea", "sovereignty", "people", "power"));

            Console.WriteLine(string.Format(KoreanFormatInfo.CurrentInfo, "{0}의 {1:은/는} {2:과/와} {3:(으)로} 한다.", "대한민국", "국토", "한반도", "그 부속도서"));
            Console.WriteLine(string.Format(KoreanFormatInfo.CurrentInfo, "{0}의 {1:은/는} {2:과/와} {3:(으)로} 한다.", "Korea", "area", "Korean Peninsula", "its islands"));
        }
    }
}
