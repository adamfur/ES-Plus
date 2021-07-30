namespace ESPlus.MoonGoose
{
    public enum ErrorCode
    {
        MDB_SUCCESS = 0,
        MDB_KEYEXIST = -30799,
        MDB_NOTFOUND = -30798,
        MDB_PAGE_NOTFOUND = -30797,
        MDB_CORRUPTED = -30796,
        MDB_PANIC = -30795,
        MDB_VERSION_MISMATCH = -30794,
        MDB_INVALID = -30793,
        MDB_MAP_FULL = -30792,
        MDB_DBS_FULL = -30791,
        MDB_READERS_FULL = -30790,
        MDB_TLS_FULL = -30789,
        MDB_TXN_FULL = -30788,
        MDB_CURSOR_FULL = -30787,
        MDB_PAGE_FULL = -30786,
        MDB_MAP_RESIZED = -30785,
        MDB_INCOMPATIBLE = -30784,
        MDB_BAD_RSLOT = -30783,
        MDB_BAD_TXN = -30782,
        MDB_BAD_VALSIZE = -30781,
        MDB_BAD_DBI = -30780,
    }
}