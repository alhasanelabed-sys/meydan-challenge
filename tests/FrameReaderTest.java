package app.meydan;
import java.io.*;import java.nio.charset.StandardCharsets;
public class FrameReaderTest {
 public static void main(String[] args)throws Exception {
  String text="{\"type\":\"answer\",\"name\":\"مريم\"}";
  if(!text.equals(FrameReader.read(new ByteArrayInputStream((text+"\n").getBytes(StandardCharsets.UTF_8)))))throw new AssertionError("Arabic frame corrupted");
  if(FrameReader.read(new ByteArrayInputStream(new byte[0]))!=null)throw new AssertionError("EOF not handled");
  for(byte[] bytes:new byte[][]{new byte[]{(byte)0xff,10},new byte[]{65},(new String(new char[65537]).replace('\0','x')+"\n").getBytes(StandardCharsets.UTF_8)}){
   boolean rejected=false;try{FrameReader.read(new ByteArrayInputStream(bytes));}catch(IOException ex){rejected=true;}if(!rejected)throw new AssertionError("Invalid frame accepted");
  }
  System.out.println("PASS: Arabic UTF-8, EOF, malformed UTF-8, truncated and oversized frames");
 }
}
