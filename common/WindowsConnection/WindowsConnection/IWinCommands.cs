namespace Sintering {

  public interface IWinCommands {

    // common parts
    ConnectionHandler connection { get; set; }

    void execute_stop_scraping();

    // server related calls
    void execute_ls_req(Sinter sinter);
    void execute_ls_l_req(Sinter sinter);
    void execute_delta(Sinter sinter) ;
    void execute_event(Sinter sinter);

    // client related calls
    void execute_ls_res(Sinter sinter);
    void execute_ls_l_res(Sinter sinter);
    void execute_action(Sinter sinter);
    void execute_kbd(Sinter sinter);
    void execute_mouse(Sinter sinter);
    void execute_listener(Sinter sinter);
  }
}