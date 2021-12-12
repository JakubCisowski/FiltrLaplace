using System.IO;

namespace InterfejsUzytkownikaCs
{
	public static class CzytnikPlikow
	{
		public static byte[] PrzeczytajBitmapeZPliku(string sciezka)
		{
			byte[] bitmapa = File.ReadAllBytes(sciezka);

			return bitmapa;
		}
	}
}
