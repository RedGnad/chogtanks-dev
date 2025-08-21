#ifndef Shared_h
#define Shared_h
inline NSString* toString(const char* string)
{
    if (string != NULL)
    {
        return [NSString stringWithUTF8String:string];
    }
    else
    {
        return [NSString stringWithUTF8String:""];
    }
}

inline char* toString(NSString* string)
{
    const char* cstr = [string UTF8String];

    if (cstr == NULL)
        return NULL;

    char* copy = (char*)malloc(strlen(cstr) + 1);
    strcpy(copy, cstr);
    return copy;
}
#endif
