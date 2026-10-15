package app.meydan;
import java.io.*;
import java.nio.ByteBuffer;
import java.nio.charset.*;
public final class FrameReader {
 public static final int MAX=65536;
 public static String read(InputStream in) throws IOException {
  ByteArrayOutputStream b=new ByteArrayOutputStream();int c;
  while((c=in.read())!=-1){if(c==10){try{return StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT).decode(ByteBuffer.wrap(b.toByteArray())).toString();}catch(CharacterCodingException ex){throw new IOException("Invalid UTF-8",ex);}}if(b.size()>=MAX)throw new IOException("Frame too large");b.write(c);}
  if(b.size()>0)throw new IOException("Truncated frame");return null;
 }
}
